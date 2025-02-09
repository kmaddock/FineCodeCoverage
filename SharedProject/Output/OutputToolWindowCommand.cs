﻿using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace FineCodeCoverage.Output
{
	/// <summary>
	/// Command handler
	/// </summary>
	internal sealed class OutputToolWindowCommand
	{
		/// <summary>
		/// Command ID.
		/// </summary>
		public const int CommandId = 0x0100;

		/// <summary>
		/// Command menu group (command set GUID).
		/// </summary>
		public static readonly Guid CommandSet = new Guid("bedda1f3-0d8f-4f8d-a818-0b5523ee662d");

		/// <summary>
		/// VS Package that provides this command, not null.
		/// </summary>
		private readonly AsyncPackage package;

		private readonly ILogger logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="OutputToolWindowCommand"/> class.
		/// Adds our command handlers for menu (commands must exist in the command table file)
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		/// <param name="commandService">Command service to add command to, not null.</param>
		private OutputToolWindowCommand(AsyncPackage package, OleMenuCommandService commandService, ILogger logger)
		{
			this.logger = logger;
			this.package = package ?? throw new ArgumentNullException(nameof(package));
			commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

			var menuCommandID = new CommandID(CommandSet, CommandId);
			var menuItem = new MenuCommand(this.Execute, menuCommandID);
			commandService.AddCommand(menuItem);
		}

		/// <summary>
		/// Gets the instance of the command.
		/// </summary>
		public static OutputToolWindowCommand Instance
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the service provider from the owner package.
		/// </summary>
		public IAsyncServiceProvider ServiceProvider
		{
			get
			{
				return package;
			}
		}

		/// <summary>
		/// Initializes the singleton instance of the command.
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		public static async Task InitializeAsync(AsyncPackage package, ILogger logger)
		{
			// Switch to the main thread - the call to AddCommand in OutputToolWindowCommand's constructor requires
			// the UI thread.
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

			OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
			Instance = new OutputToolWindowCommand(package, commandService, logger);
		}

		/// <summary>
		/// Shows the tool window when the menu item is clicked.
		/// </summary>
		/// <param name="sender">The event sender.</param>
		/// <param name="e">The event args.</param>
		public void Execute(object sender, EventArgs e)
		{
            _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
                try
                {
                    await ShowToolWindowAsync();
                }
                catch (Exception exception)
                {
                    logger.Log(exception);
                }
            });

        }

		public async Task<ToolWindowPane> ShowToolWindowAsync()
		{
            ToolWindowPane window = await package.ShowToolWindowAsync(typeof(OutputToolWindow), 0, true, package.DisposalToken);

			return ReturnOrThrowIfCannotCreateToolWindow(window);
		}

		public async Task<ToolWindowPane> FindToolWindowAsync()
		{
            ToolWindowPane window = await package.FindToolWindowAsync(typeof(OutputToolWindow), 0, true, package.DisposalToken);

            return ReturnOrThrowIfCannotCreateToolWindow(window);
		}

		private ToolWindowPane ReturnOrThrowIfCannotCreateToolWindow(ToolWindowPane window)
        {
			if ((null == window) || (null == window.Frame))
			{
				throw new NotSupportedException($"Cannot create '{Vsix.Name}' output window");
			}

			return window;
		}
	}
}
