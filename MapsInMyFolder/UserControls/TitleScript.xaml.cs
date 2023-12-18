using ICSharpCode.AvalonEdit.Highlighting;
using MapsInMyFolder.Commun;
using System;
using System.Windows;
using System.Windows.Controls;

namespace MapsInMyFolder.UserControls
{
    public partial class TitleScript : UserControl
    {
        public TitleScript()
        {
            InitializeComponent();
        }

        public Label Label
        {
            get { return TitleScriptLabel; }
            set { TitleScriptLabel = value; }
        }

        public string ScriptName
        {
            get { return NameTextbox.Text; }
            set { NameTextbox.Text = value; }
        }

        public string Script
        {
            get { return ScriptTextEditor.Script; }
            set { ScriptTextEditor.Script = value; }
        }

        public IHighlightingDefinition SyntaxHighlighting
        {
            get { return ScriptTextEditor.SyntaxHighlighting; }
            set { ScriptTextEditor.SyntaxHighlighting = value; }
        }

        public void AddToContextMenu(string Header, string Icon, System.Windows.RoutedEventHandler action)
        {
            ScriptTextEditor.AddToContextMenu(Header, Icon, action);
        }

        public void SetTextEditorPositionChangedHandler(Grid grid, ScrollViewer scrollViewer, int scrollOffset)
        {
            ScriptTextEditor.SetTextEditorPositionChangedHandler(grid, scrollViewer, scrollOffset);
        }

        public void Indent()
        {
            ScriptTextEditor.Indent();
        }

        private void TitleScriptDelete_Click(object sender, RoutedEventArgs e)
        {
            // Find the parent StackPanel
            StackPanel stackPanel = Collectif.FindParent<StackPanel>(this);

            // Remove this control from the StackPanel
            if (stackPanel != null)
            {
                stackPanel.Children.Remove(this);
            }
            else
            {
                // Handle different parent types accordingly
                throw new InvalidOperationException("Parent container is not a StackPanel.");
            }
        }
    }
}
