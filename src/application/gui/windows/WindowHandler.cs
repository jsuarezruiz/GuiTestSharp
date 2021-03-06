﻿using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

using log4net;
using log4net.Config;
using log4net.Repository;

using GuiTest;

using Codice.Examples.GuiTesting.GuiTestInterfaces;
using Codice.Examples.GuiTesting.Lib;
using Codice.Examples.GuiTesting.Windows.Testing;

namespace Codice.Examples.GuiTesting.Windows
{
    internal static class WindowHandler
    {
        internal static IWin32Window ApplicationWindow
        {
            get { return mApplicationWindow; }
        }

        internal static void LaunchApplicationWindow()
        {
            ApplicationOperations operations = new ApplicationOperations();
            mApplicationWindow = new ApplicationWindow(operations);
            mApplicationWindow.StartPosition = FormStartPosition.CenterScreen;
            mApplicationWindow.Show();
        }

        internal static void UnregisterApplicationWindow()
        {
            mApplicationWindow.Dispose();
            mApplicationWindow = null;

            TerminateApplication();
        }

        internal static void SetActiveDialogForTesting(Form dialog)
        {
            if (!mbIsTestRun)
                return;

            mActiveDialog = dialog;
        }

        internal static void RemoveDialogForTesting(Form dialog)
        {
            if (!mbIsTestRun)
                return;

            if (mActiveDialog == dialog)
                mActiveDialog = null;
        }

        internal static Form GetActiveDialog()
        {
            if (mActiveDialog == null)
                return null;

            // Although by this point the Form is created, it might not be
            // visible yet, so it's handle is not created.
            // If the handle is not created, all Invoke and BeginInvoke calls
            // will fail, so we'll have to wait.
            while (!mActiveDialog.IsHandleCreated) { }

            return mActiveDialog;
        }

        internal static void LaunchTest(string testInfoFile, string pathToAssemblies)
        {
            mbIsTestRun = true;

            ConfigureTestLogging();
            InitTesteableServices();

            GuiTestRunner testRunner = new GuiTestRunner(
                testInfoFile, pathToAssemblies, new GuiFinalizer(), null);

            ThreadPool.QueueUserWorkItem(new WaitCallback(testRunner.Run));
        }

        static void ConfigureTestLogging()
        {
            try
            {
                string log4netPath = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "pnunittestrunner.log.conf");

                XmlConfigurator.Configure(mLogRepository, new FileInfo(log4netPath));
            }
            catch { } // Log config failed, nothing else to do!
        }

        static void InitTesteableServices()
        {
            TesteableApplicationWindow testeableWindow =
                new TesteableApplicationWindow(mApplicationWindow);

            GuiTesteableServices.Init(testeableWindow);
        }

        static void TerminateApplication()
        {
            if (mbIsTestRun)
                return;

            Application.Exit();
        }

        static ApplicationWindow mApplicationWindow;
        static Form mActiveDialog;
        static bool mbIsTestRun = false;

        static readonly ILoggerRepository mLogRepository =
            LogManager.CreateRepository("log4net-rep");

        class GuiFinalizer : GuiTestRunner.IGuiFinalizer
        {
            void GuiTestRunner.IGuiFinalizer.Finalize(int exitCode)
            {
                Environment.Exit(exitCode);
            }
        }
    }
}
