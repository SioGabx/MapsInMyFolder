using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using MapsInMyFolder.Commun;
using ModernWpf.Controls;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MapsInMyFolder.UserControls
{
    /// <summary>
    /// Logique d'interaction pour ScriptEditor.xaml
    /// </summary>
    public partial class ScriptEditor : UserControl
    {
        public ScriptEditor()
        {
            InitializeComponent();
        }


        private void DisableRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;
        }

        private static void SetFixMouseWheelBehavior(TextEditor textEditor)
        {
            ScrollViewerHelper.SetScrollViewerMouseWheelFix(Collectif.GetDescendantByType(textEditor, typeof(ScrollViewer)) as ScrollViewer);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            SetFixMouseWheelBehavior(ScriptTextEditor);
            SetCaretBrush(ScriptTextEditor);
            ScriptTextEditor.TextArea.Options.ConvertTabsToSpaces = true;
            ScriptTextEditor.TextArea.Options.IndentationSize = 4;
        }

        public void SetTextEditorPositionChangedHandler(Grid grid, ScrollViewer scrollViewer, int scrollOffset)
        {
            void textEditor_PositionChanged(object sender, EventArgs e)
            {
                Collectif.TextEditorCursorPositionChanged(ScriptTextEditor, grid, scrollViewer, scrollOffset);
            }

            void textEditor_Unloaded(object sender, EventArgs e)
            {
                ScriptTextEditor.TextArea.Caret.PositionChanged -= textEditor_PositionChanged;
                ScriptTextEditor.Unloaded -= textEditor_Unloaded;
            }

            ScriptTextEditor.TextArea.Caret.PositionChanged += textEditor_PositionChanged;
            ScriptTextEditor.Unloaded += textEditor_Unloaded;
        }

        private static void SetCaretBrush(TextEditor textEditor)
        {
            textEditor.TextArea.Caret.CaretBrush = Collectif.HexValueToSolidColorBrush("#f18712");
        }

        public void Indent()
        {
            Commun.JSBeautify jSBeautify = new JSBeautify(Script, new JSBeautifyOptions() { preserve_newlines = false, indent_char = ' ', indent_size = 4 });
            Script = jSBeautify.GetResult();
        }

        public void InsertTextAtCaretPosition(string text)
        {
            int CaretIndex = ScriptTextEditor.CaretOffset;
            if (ScriptTextEditor.SelectionLength == 0)
            {
                ScriptTextEditor.TextArea.Document.Insert(CaretIndex, text);
            }
            else
            {
                ScriptTextEditor.SelectedText = text;
                ScriptTextEditor.SelectionLength = 0;
            }

            ScriptTextEditor.CaretOffset = Math.Min(ScriptTextEditor.Text.Length, CaretIndex + text.Length);
        }


        public new event KeyEventHandler KeyUp;
        public event EventHandler TextChanged;

        public string Script
        {
            get { return ScriptTextEditor.Text; }
            set { ScriptTextEditor.Text = value; }
        }

        public void SetScriptNoEvent(string text)
        {
            ScriptTextEditor.TextArea.Document.Text = text;
        }

        public new ContextMenu ContextMenu
        {
            get { return ScriptTextEditor.ContextMenu; }
            set { ScriptTextEditor.ContextMenu = value; }
        }

        public new double MinHeight
        {
            get { return ScriptTextEditor.MinHeight; }
            set { ScriptTextEditor.MinHeight = value; }
        }

        public void AddToContextMenu(string Header, string Icon, System.Windows.RoutedEventHandler action)
        {
            MenuItem indenterMenuItem = new MenuItem
            {
                Header = Header,
                Icon = new FontIcon() { Glyph = Icon, Foreground = Collectif.HexValueToSolidColorBrush("#888989") }
            };

            void indenterMenuItem_Unloaded(object sender, EventArgs e)
            {
                indenterMenuItem.Click -= action;
                ScriptTextEditor.ContextMenu.Items.Remove(indenterMenuItem);
                ScriptTextEditor.Unloaded -= indenterMenuItem_Unloaded;
            }

            indenterMenuItem.Click += action;
            ScriptTextEditor.Unloaded += indenterMenuItem_Unloaded;
            ScriptTextEditor.ContextMenu.Items.Add(indenterMenuItem);
        }

        public IHighlightingDefinition SyntaxHighlighting
        {
            get { return ScriptTextEditor.SyntaxHighlighting; }
            set { ScriptTextEditor.SyntaxHighlighting = value; }
        }

        private void ScriptTextEditor_KeyUp(object sender, KeyEventArgs e)
        {
            KeyUp?.Invoke(this, e);
        }

        private void ScriptTextEditor_TextChanged(object sender, EventArgs e)
        {
            TextChanged?.Invoke(this, e);
        }
    }
}
