# Termix Setup Guide

## Prerequisites

- Microsoft Windows 10 Home (fully updated)
- Microsoft Visual Studio Community 2017 (fully updated)
  - Check ".NET desktop development" in "Workloads" tab of Visual Studio Installer
  - Check "Git for Windows" in "Individual components" tab (in "Code tools" section) of Visual Studio Installer

If you decide to use different software or if the software undergoes a singificant update, it is possible that some parts of the setup process may change. If that happens, please let me know via a GitHub message or an email.

## Compilation

1. Open Command Prompt and navigate to a suitable directory
2. Type `git clone https://github.com/natiiix/termix` (this will download the source files into a directory `termix/`)
3. Open Visual Studio
4. Click on "Open Project / Solution"
5. Navigate to the `termix/` directory, select `Termix.sln` and click "Open"
6. Press Ctrl+Shift+B (this will built the solution into `termix/Termix/bin/Debug/Termix.exe`)

## Using Termix

Termix uses Google Cloud Platform (more specifically Google Cloud Speech), which means that in order to be able to use it properly, you need to install Google Cloud Platform SDK.

The official installation guide is [here](https://cloud.google.com/sdk/docs/quickstart-windows).

However I have written a simplified verison of the guide for you. It is limited to parts needed for Termix to work.

1. Download the Cloud SDK installer from [here](https://dl.google.com/dl/cloudsdk/channels/rapid/GoogleCloudSDKInstaller.exe)
2. Install the Cloud SDK (there is nothing to explain about that, you just need to click "Next" a couple of times)
3. At the end of the installation check (or keep checked) the option that says "Run 'gcloud init' to configure Cloud SDK" and click "Finish"
4. The Cloud SDK initialization will ask you to log into your Google account, so please do that
5. Once you have logged in, the Cloud SDK initialization will ask you to select a project
   - If you already have created a Google Cloud Platform project before and want to use it, you can select it by typing its number
   - Otherwise type whatever number corresponds to the "Create a new project" choice, enter a project name and confirm it
6. Congratulations! You should now be able to run Termix by clicking the "Start" button in Visual Studio or by double-clicking the `Termix.exe` executable
