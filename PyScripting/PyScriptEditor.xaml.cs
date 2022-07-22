using System;
using System.IO;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Win32;
using System.Threading;
using System.Windows.Threading;
using System.Reflection;

using ActiproSoftware.Text.Languages.Python;
using ActiproSoftware.Text.Languages.Python.Implementation;
using ActiproSoftware.Text.Parsing;
using ActiproSoftware.Text.Parsing.LLParser;
using ActiproSoftware.Windows.Controls.SyntaxEditor;
using ActiproSoftware.Text;

using PyScripting.ScriptingEngine;

#if WPF
using MessageBox = ActiproSoftware.Windows.Controls.ThemedMessageBox;
#endif

namespace PyScripting {

	/// <summary>
	/// Provides the main user control for this sample.
	/// </summary>
	public partial class PyScriptEditor : UserControl {

		private int					documentNumber;
		private bool				hasPendingParseData;

		////////////////////////////////////////////////
		// Musa-added-private-fields
		///////////////////////////////////////////////
		private Thread dispatcherThread;
		private Window dispatcherWindow;
		private Dispatcher dispatcher;
		private PyInterpreter interpreter;
		private bool saved = false;
		private bool dirty = false;
		private bool executing = false;
		////////////////////////////////////////////////

		/////////////////////////////////////////////////////////////////////////////////////////////////////
		// OBJECT
		/////////////////////////////////////////////////////////////////////////////////////////////////////

		/// <summary>
		/// Initializes an instance of the <c>PyScriptEditor</c> class.
		/// </summary>
		public PyScriptEditor() {
			InitializeComponent();

			
			dispatcherThread = new Thread(new ThreadStart(DispatcherThreadStartingPoint));
			dispatcherThread.SetApartmentState(ApartmentState.STA);
			dispatcherThread.IsBackground = true;
			dispatcherThread.Start();

			console.Document.SetText(">>> ");
			//console.Document.AppendText(TextChangeTypes.Enter, "\n");
			MoveToEnd();


			codeEditor.Document.Language = new PythonSyntaxLanguage();
			codeEditor.Document.FileName = String.Format("Document{0}.py", documentNumber);
			label.Header = codeEditor.Document.FileName;

			// Check if python is installed and get the path
			var project = codeEditor.Document.Language.GetProject();
			var path = Utils.TryGetFullPathFromPathEnvironmentVariable("python.exe");
			if (path != null)
            {
				var directoryPath = Path.GetDirectoryName(path);
				var libPath = Path.Combine(directoryPath, "Lib");
				var sitePackagesPath = Path.Combine(libPath, "site-packages");
				//var project = codeEditor.Document.Language.GetProject();
				project.SearchPaths.Clear();
				if(Directory.Exists(libPath))
                {
					project.SearchPaths.Add(libPath);
				}
				if(Directory.Exists(sitePackagesPath))
                {
					project.SearchPaths.Add(sitePackagesPath);
				}
				
			}
			else
            {
				MessageBox.Show("Couldn't locate python installation directory!!!");
			}

			// Add startup script to search directory for intelliscence
			var dir = AppDomain.CurrentDomain.BaseDirectory;
			if(Directory.Exists(dir))
            {
				project.SearchPaths.Add(dir);
			}

			//

		}

		/////////////////////////////////////////////////////////////////////////////////////////////////////
		// NON-PUBLIC PROCEDURES
		/////////////////////////////////////////////////////////////////////////////////////////////////////

		/// <summary>
		/// Creates a new XML file.
		/// </summary>
		private void NewFile() {
			this.OpenFile(String.Format("Document{0}.py", ++documentNumber), null);
		}
		
		/// <summary>
		/// Occurs when the document's parse data has changed.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		/// <param name="e">The <c>EventArgs</c> that contains data related to this event.</param>
		private void OnCodeEditorDocumentParseDataChanged(object sender, EventArgs e) {
			//
			// NOTE: The parse data here is generated in a worker thread... this event handler is called 
			//         back in the UI thread immediately when the worker thread completes... it is best
			//         practice to delay UI updates until the end user stops typing... we will flag that
			//         there is a pending parse data change, which will be handled in the 
			//         UserInterfaceUpdate event
			//

			hasPendingParseData = true;
		}

		/// <summary>
		/// Occurs after a brief delay following any document text, parse data, or view selection update, allowing consumers to update the user interface during an idle period.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		/// <param name="e">The <see cref="RoutedEventArgs"/> that contains data related to this event.</param>
		private void OnCodeEditorUserInterfaceUpdate(object sender, RoutedEventArgs e) {
			// If there is a pending parse data change...
			if (hasPendingParseData) {
				// Clear flag
				hasPendingParseData = false;

				ILLParseData parseData = codeEditor.Document.ParseData as ILLParseData;
				if (parseData != null) {
					//if (codeEditor.Document.CurrentSnapshot.Length < 10000) {
					//	// Show the AST
					//	if (parseData.Ast != null)
					//		astOutputEditor.Document.SetText(parseData.Ast.ToTreeString(0));
					//	else
					//		astOutputEditor.Document.SetText(null);
					//}
					//else
					//	astOutputEditor.Document.SetText("(Not displaying large AST for performance reasons)");

					// Output errors
					errorListView.ItemsSource = parseData.Errors;
					label.Header = Path.GetFileName(codeEditor.Document.FileName) + "*";
					dirty = true;
				}
				else {
					// Clear UI
					astOutputEditor.Document.SetText(null);
					errorListView.ItemsSource = null;
				}
			}
		}

		/// <summary>
		/// Occurs when the document's view selection has changed.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		/// <param name="e">The <see cref="EditorViewSelectionEventArgs"/> that contains data related to this event.</param>
		private void OnCodeEditorViewSelectionChanged(object sender, EditorViewSelectionEventArgs e) {
			// Quit if this event is not for the active view
			if (!e.View.IsActive)
				return;

			// Update line, col, and character display
			linePanel.Text = String.Format("Ln {0}", e.CaretPosition.DisplayLine);
			columnPanel.Text = String.Format("Col {0}", e.CaretDisplayCharacterColumn);
			characterPanel.Text = String.Format("Ch {0}", e.CaretPosition.DisplayCharacter);
		}

		/// <summary>
		/// Occurs when a mouse is double-clicked.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		/// <param name="e">A <see cref="MouseButtonEventArgs"/> that contains the event data.</param>
		private void OnErrorListViewDoubleClick(object sender, MouseButtonEventArgs e) {
			ListBox listBox = (ListBox)sender;
			IParseError error = listBox.SelectedItem as IParseError;
			if (error != null) {
				codeEditor.ActiveView.Selection.StartPosition = error.PositionRange.StartPosition;
				codeEditor.Focus();
			}
		}
		
		/// <summary>
		/// Occurs when the button is clicked.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		/// <param name="e">A <see cref="RoutedEventArgs"/> that contains the event data.</param>
		private void OnNewFileButtonClick(object sender, RoutedEventArgs e) {
			this.NewFile();
		}

		/// <summary>
		/// Occurs when the button is clicked.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		/// <param name="e">A <see cref="RoutedEventArgs"/> that contains the event data.</param>
		private void OnOpenFileButtonClick(object sender, RoutedEventArgs e) {
			// Show a file open dialog
			OpenFileDialog dialog = new OpenFileDialog();
			if (!BrowserInteropHelper.IsBrowserHosted)
				dialog.CheckFileExists = true;
			dialog.Multiselect = false;
			dialog.Filter = "Python files (*.py)|*.py|All files (*.*)|*.*";
			if (dialog.ShowDialog() == true) {
				// Open a document (use dialog to help open the file because of security restrictions in XBAP/Silverlight)
				using (Stream stream = dialog.OpenFile()) {
					// Read the file
					this.OpenFile(dialog.FileName, stream);
				}

				saved = true;
			}
		}
		
		/// <summary>
		/// Occurs when the button is clicked.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		/// <param name="e">A <see cref="RoutedEventArgs"/> that contains the event data.</param>
		private void OnOpenStandardLibraryButtonClick(object sender, RoutedEventArgs e) {
			// Show a file open dialog
			OpenFileDialog dialog = new OpenFileDialog();
			if (!BrowserInteropHelper.IsBrowserHosted) {
				dialog.CheckFileExists = true;
				dialog.Title = "Select a file from the Lib folder of your Python standard library";
			}
			dialog.Multiselect = false;
			dialog.Filter = "Python files (*.py)|*.py|All files (*.*)|*.*";
			if (dialog.ShowDialog() == true) {
				try {
					var directoryPath = Path.GetDirectoryName(dialog.FileName);
					if (Directory.Exists(directoryPath)) {
						// Add the containing directory as a search path
						var project = codeEditor.Document.Language.GetProject();
						project.SearchPaths.Clear();
						project.SearchPaths.Add(directoryPath);

						// Add the site-packages child folder, if present
						var sitePackagesPath = Path.Combine(directoryPath, "site-packages");
						if (Directory.Exists(sitePackagesPath))
							project.SearchPaths.Add(sitePackagesPath);

						MessageBox.Show("Standard library location set to '" + directoryPath + "'.");
					}
				}
				catch (ArgumentException) {}
				catch (IOException) {}
				catch (SecurityException) {}
			}
		}

		/// <summary>
		/// Opens a file.
		/// </summary>
		/// <param name="filename">The filename.</param>
		/// <param name="stream">The <see cref="PyStream"/> to load.</param>
		private void OpenFile(string filename, Stream stream) {
			// Load the file
			if (stream != null)
				codeEditor.Document.LoadFile(stream, Encoding.UTF8);
			else
				codeEditor.Document.SetText(null);

			// Set the filename
			codeEditor.Document.FileName = filename;
		}

		////////////////////////////////////////////////////////////////////
		/// My Codes
		/// ////////////////////////////////////////////////////////////////

		private void OnSaveFileButtonClick(object sender, RoutedEventArgs e)
        {

			OnSaveFileButtonClickHelper();

			//if (!saved)
            //{
			//	SaveFile();
            //}
			//else
            //{
				//if(dirty)
                //{
					//var filename = codeEditor.Document.FileName;
					//codeEditor.Document.SaveFile(filename, LineTerminator.Newline);
					//label.Header = Path.GetFileName(filename);
					//dirty = false;
				//}
            //}
			
		}

		private void OnSaveFileButtonClickHelper()
        {
			if (!saved)
			{
				SaveFile();
			}
			else
			{
				if (dirty)
				{
					var filename = codeEditor.Document.FileName;
					codeEditor.Document.SaveFile(filename, LineTerminator.Newline);
					label.Header = Path.GetFileName(filename);
					dirty = false;
				}
			}
		}

		private void OnSaveAsFileButtonClick(object sender, RoutedEventArgs e)
		{
			SaveFile();
		}

		private void SaveFile()
        {
			// Show a file open dialog
			SaveFileDialog dialog = new SaveFileDialog();
			//if (!BrowserInteropHelper.IsBrowserHosted)
			//dialog.CheckFileExists = true;
			dialog.Filter = "Python files (*.py)|*.py|All files (*.*)|*.*";
			dialog.FileName = codeEditor.Document.FileName;
			if (dialog.ShowDialog() == true)
			{
				codeEditor.Document.SaveFile(dialog.FileName, LineTerminator.Newline);
				dirty = false;
				saved = true;
			}
			codeEditor.Document.FileName = dialog.FileName;
			label.Header = Path.GetFileName(codeEditor.Document.FileName);
		}
		private void CWrite(object sender, WriteEventArgs e)
		{
			Write(e.Data);
		}

		private void OnRunButtonClick(object sender, EventArgs args)
		{
			OnSaveFileButtonClickHelper();
			var src = codeEditor.Text;
			RunStatements(src);
			//interpreter.RunSource(src);
			//Thread accept = new Thread(() =>
			//{
			//    interpreter.RunSource(src);
			//});
			//accept.Start();     
		}

		private void DispatcherThreadStartingPoint()
		{
			dispatcherWindow = new Window();
			dispatcher = dispatcherWindow.Dispatcher;
			var outStream = new PyStream();
			outStream.WriteEvent += CWrite;
			interpreter = new PyInterpreter(outStream);
			while (true)
			{
				try
				{
					Dispatcher.Run();
				}
				catch
				{
					// ToDo: Catch interupt generated exception here.

				}
			}
		}

		public void RunStatements(string statements)
		{
			dispatcher.BeginInvoke(new Action(delegate () { ExecuteStatements(statements); }));
		}

		void ExecuteStatements(string scriptText)
		{
			lock (scriptText)
			{
				//MoveToEnd();
				////CodeTextEditor.Write("\r\n");
				//PerformTextInput("\r\n");
				Write("\r\n");
				string error = "";
				try
				{
					executing = true;
					interpreter.RunSource(scriptText);
				}
				catch (Python.Runtime.PythonException e)
				{
					error = e.Message + Environment.NewLine;
				}

				executing = false;
				if (error != "")
				{
					//MoveToEnd();
					//PerformTextInput(error);
					Write(error);
				}


				Write(">>> ");

			}
		}


		private void PerformTextInput(string text)
		{
			if (text == "\n" || text == "\r\n")
			{
				//string newLine = console.Document.
				console.Document.AppendText(TextChangeTypes.Enter, "\n");

				//using (textArea.Document.RunUpdate())
				//{

				//    textArea.Selection.ReplaceSelectionWithText(textArea, newLine);
				//}
			}
			else
				console.Document.AppendText(TextChangeTypes.Typing, text);
			//console.TextArea.Caret.BringCaretToView();
		}

		private void MoveToEnd()
		{
			//int lineCount = console.TextArea.Document.LineCount;
			int lineCount = console.Document.CurrentSnapshot.Lines.Count;
			var line = console.Document.CurrentSnapshot.Lines[lineCount - 1];
			int column = line.Length;
			console.ActiveView.Selection.CaretPosition = new TextPosition(lineCount-1, column);
		
		}

		private void Write(string text)
		{
			console.Dispatcher.BeginInvoke(new Action(() =>
			{
				MoveToEnd();
				PerformTextInput(text);
			}));
		}



	}

}