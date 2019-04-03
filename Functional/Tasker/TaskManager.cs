using FluentScheduler;

namespace Exceling.Functional.Tasker
{
    public class TaskManager
    {
        private LogProgram logger;

        public TaskManager(LogProgram logger)
        {
            this.logger = logger;
            SetNewLogFile();
        }
        public bool EmailMonitoring(Worker worker)
        {
            worker.HandleDownloadXlsByEmail();
            Registry registry = new Registry();
            registry.Schedule(() => worker.HandleDownloadXlsByEmail()).ToRunEvery(1).Days().At(6,0);
            logger.WriteLog("Task manager start EmailMonitoring method", LogLevel.Tasker);
            JobManager.InitializeWithoutStarting(registry);
            JobManager.Start();
            return true;
        }
        private void SetNewLogFile()
        {
            Registry registry = new Registry();
            registry.Schedule(() => logger.SetUpNewLogFile()).ToRunEvery(1).Days();
            logger.WriteLog("Task manager start SetNewLogFile()", LogLevel.Tasker);
            JobManager.InitializeWithoutStarting(registry);
            JobManager.Start();
        }
    }
}
