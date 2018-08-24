namespace CHC_Image_Builder
{
    public class PowershellHelper
    {
        public void Execute(string command)
        {
            using (var ps = PowerShell.Create())
            {
                var results = ps.AddScript(command).Invoke();
                foreach (var result in results)
                {
                    Logger.log.Info(result.ToString());
                }
            }
        }
    }
}