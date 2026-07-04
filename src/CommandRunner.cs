using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace FloatingHud;

public sealed class CommandRunner : IDisposable
{
    private readonly object syncRoot = new();
    private readonly SemaphoreSlim runGate = new(1, 1);
    private CommandExecution? activeExecution;
    private bool isDisposed;

    public async Task<CommandResult> RunAsync(string commandLine)
    {
        await runGate.WaitAsync();
        CommandExecution? execution = null;

        try
        {
            ThrowIfDisposed();

            execution = CommandExecution.Start(commandLine);
            lock (syncRoot)
            {
                if (isDisposed)
                {
                    throw new ObjectDisposedException(nameof(CommandRunner));
                }

                activeExecution = execution;
            }

            Task<string> stdoutTask = execution.Process.StandardOutput.ReadToEndAsync();
            Task<string> stderrTask = execution.Process.StandardError.ReadToEndAsync();
            await execution.Process.WaitForExitAsync();
            string stdout = await stdoutTask;
            string stderr = await stderrTask;
            return new CommandResult(
                execution.TimedOut ? -1 : execution.Process.ExitCode,
                stdout,
                stderr,
                execution.TimedOut);
        }
        finally
        {
            lock (syncRoot)
            {
                if (execution is not null && ReferenceEquals(activeExecution, execution))
                {
                    activeExecution = null;
                }
            }

            execution?.Dispose();
            runGate.Release();
        }
    }

    public void TimeoutActiveExecution()
    {
        CommandExecution? executionToStop;
        lock (syncRoot)
        {
            executionToStop = activeExecution;
        }

        executionToStop?.MarkTimedOut();
        executionToStop?.Terminate();
    }

    public void Dispose()
    {
        CommandExecution? executionToStop;
        lock (syncRoot)
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            executionToStop = activeExecution;
            activeExecution = null;
        }

        executionToStop?.Terminate();
        executionToStop?.Dispose();
    }

    private void ThrowIfDisposed()
    {
        lock (syncRoot)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(CommandRunner));
            }
        }
    }

    private sealed class CommandExecution : IDisposable
    {
        private const uint JobObjectLimitKillOnJobClose = 0x00002000;
        private const int JobObjectExtendedLimitInformationClass = 9;
        private bool isDisposed;

        private CommandExecution(Process process, SafeJobHandle jobHandle)
        {
            Process = process;
            this.JobHandle = jobHandle;
        }

        public Process Process { get; }

        public bool TimedOut { get; private set; }

        private SafeJobHandle JobHandle { get; }

        public static CommandExecution Start(string commandLine)
        {
            SafeJobHandle jobHandle = CreateKillOnCloseJob();
            Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe",
                    Arguments = $"/d /s /c {commandLine}",
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                },
            };

            try
            {
                process.Start();
                if (!AssignProcessToJobObject(jobHandle, process.Handle))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "无法将命令进程加入 Job Object。");
                }

                return new CommandExecution(process, jobHandle);
            }
            catch
            {
                KillProcessIfRunning(process);
                process.Dispose();
                jobHandle.Dispose();
                throw;
            }
        }

        private static void KillProcessIfRunning(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
            catch (Win32Exception)
            {
            }
        }

        public void MarkTimedOut()
        {
            try
            {
                Process.Refresh();
                TimedOut = TimedOut || !Process.HasExited;
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
            catch (Win32Exception)
            {
            }
        }

        public void Terminate()
        {
            if (isDisposed)
            {
                return;
            }

            TerminateJobObject(JobHandle, 1);
            try
            {
                if (!Process.HasExited)
                {
                    Process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
            }
            catch (Win32Exception)
            {
            }
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            JobHandle.Dispose();
            Process.Dispose();
        }

        private static SafeJobHandle CreateKillOnCloseJob()
        {
            SafeJobHandle jobHandle = CreateJobObject(nint.Zero, null);
            if (jobHandle.IsInvalid)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "无法创建 Job Object。");
            }

            JobObjectExtendedLimitInformation info = new()
            {
                BasicLimitInformation =
                {
                    LimitFlags = JobObjectLimitKillOnJobClose,
                },
            };

            if (!SetInformationJobObject(
                jobHandle,
                JobObjectExtendedLimitInformationClass,
                ref info,
                (uint)Marshal.SizeOf<JobObjectExtendedLimitInformation>()))
            {
                int error = Marshal.GetLastWin32Error();
                jobHandle.Dispose();
                throw new Win32Exception(error, "无法配置 Job Object。");
            }

            return jobHandle;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern SafeJobHandle CreateJobObject(nint lpJobAttributes, string? lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetInformationJobObject(
            SafeJobHandle hJob,
            int jobObjectInfoClass,
            ref JobObjectExtendedLimitInformation lpJobObjectInfo,
            uint cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AssignProcessToJobObject(SafeJobHandle hJob, nint hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool TerminateJobObject(SafeJobHandle hJob, uint uExitCode);

        [StructLayout(LayoutKind.Sequential)]
        private struct JobObjectBasicLimitInformation
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IoCounters
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JobObjectExtendedLimitInformation
        {
            public JobObjectBasicLimitInformation BasicLimitInformation;
            public IoCounters IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }
    }

    private sealed class SafeJobHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeJobHandle()
            : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return CloseHandle(handle);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(nint hObject);
    }
}
