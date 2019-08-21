===============================================
How to Deploy & Run
===============================================

Make sure you have a Job Queue file (TSV format) as follows:
The file can be empty but must have the header when deploying initially

|---------------------------------------------------------------------------------------------------------------------------------------------------|
| JobId  | Timestamp             | JobStatus | JobString                                                                   | RunCount | Comments    |
|---------------------------------------------------------------------------------------------------------------------------------------------------|
|  Job#1 | 9/11/2018 17:23:00 PM | Queued    | F:\Projects\DemoJob\obj\Debug\DemoJob.exe|ComputeJob|23|104|F:\output-2.txt | 0        |	queued job. |
|---------------------------------------------------------------------------------------------------------------------------------------------------|

Configure the JobQueueService.exe.config file: e.g. Set the QueueFile path, set the logging dir, etc.

Open Developer Command Prompt for Visual Studio in Admin mode (or use "C:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe")

Type (Install)   >> installutil.exe  .\bin\Debug\JobQueueService.exe
Type (Uninstall) >> installutil.exe  /u  .\bin\Debug\JobQueueService.exe