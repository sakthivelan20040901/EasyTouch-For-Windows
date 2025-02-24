using System;
using System.Diagnostics;
using System.IO;

namespace FloatingBall
{
    public class ScreenRecorder
    {
        private Process ffmpegProcess;
        private string outputPath;

        public void StartRecording(string outputPath)
        {
            this.outputPath = outputPath;
            string ffmpegArgs = $"-f gdigrab -framerate 30 -i desktop -c:v libx264 -preset ultrafast -pix_fmt yuv420p \"{outputPath}\"";
            
            ffmpegProcess = new Process();
            ffmpegProcess.StartInfo.FileName = "ffmpeg";
            ffmpegProcess.StartInfo.Arguments = ffmpegArgs;
            ffmpegProcess.StartInfo.UseShellExecute = false;
            ffmpegProcess.StartInfo.CreateNoWindow = true;
            ffmpegProcess.StartInfo.RedirectStandardError = true;
            ffmpegProcess.Start();
        }

        public void StopRecording()
        {
            if (ffmpegProcess != null && !ffmpegProcess.HasExited)
            {
                // Send 'q' to gracefully stop ffmpeg
                ffmpegProcess.StandardInput.Write('q');
                ffmpegProcess.WaitForExit(5000);
                ffmpegProcess.Close();
            }
        }
    }
}
