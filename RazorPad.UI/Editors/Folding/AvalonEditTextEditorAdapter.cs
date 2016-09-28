using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using RazorPad.UI.Editors.CodeCompletion;
using RazorPad.UI.Editors.Services;

namespace RazorPad.UI.Editors.Folding
{
    /// <summary>
    /// Wraps AvalonEdit to provide the ITextEditor interface.
    /// </summary>
    public class AvalonEditTextEditorAdapter : ITextEditor, IWeakEventListener
    {
        readonly TextEditor textEditor;
        AvalonEditDocumentAdapter document;

        public TextEditor TextEditor
        {
            get { return textEditor; }
        }

        public AvalonEditTextEditorAdapter(TextEditor textEditor)
        {
            if (textEditor == null)
                throw new ArgumentNullException("textEditor");
            this.textEditor = textEditor;
            this.Caret = new CaretAdapter(textEditor.TextArea.Caret);
            TextEditorWeakEventManager.DocumentChanged.AddListener(textEditor, this);
            OnDocumentChanged();
        }

        
        protected virtual bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            if (managerType == typeof(TextEditorWeakEventManager.DocumentChanged))
            {
                OnDocumentChanged();
                return true;
            }
            return false;
        }

        bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            return ReceiveWeakEvent(managerType, sender, e);
        }

        void OnDocumentChanged()
        {
            if (textEditor.Document != null)
                document = new AvalonEditDocumentAdapter(textEditor.Document, this);
            else
                document = null;
        }

        public IDocument Document
        {
            get { return document; }
        }

        public ITextEditorCaret Caret { get; private set; }

        public virtual ILanguageBinding Language
        {
            get { return AggregatedLanguageBinding.NullLanguageBinding; }
        }

        sealed class CaretAdapter : ITextEditorCaret
        {
            Caret caret;

            public CaretAdapter(Caret caret)
            {
                Debug.Assert(caret != null);
                this.caret = caret;
            }

            public int Offset
            {
                get { return caret.Offset; }
                set { caret.Offset = value; }
            }

            public int Line
            {
                get { return caret.Line; }
                set { caret.Line = value; }
            }

            public int Column
            {
                get { return caret.Column; }
                set { caret.Column = value; }
            }

            public Location Position
            {
                get { return AvalonEditDocumentAdapter.ToLocation(caret.Location); }
                set { caret.Location = AvalonEditDocumentAdapter.ToPosition(value); }
            }

            public event EventHandler PositionChanged
            {
                add { caret.PositionChanged += value; }
                remove { caret.PositionChanged -= value; }
            }
        }

        public object GetService(Type serviceType)
        {
            return textEditor.TextArea.GetService(serviceType);
        }

        public int SelectionStart
        {
            get
            {
                return textEditor.SelectionStart;
            }
        }

        public int SelectionLength
        {
            get
            {
                return textEditor.SelectionLength;
            }
        }

        public string SelectedText
        {
            get
            {
                return textEditor.SelectedText;
            }
            set
            {
                textEditor.SelectedText = value;
            }
        }

        public event KeyEventHandler KeyPress
        {
            add { textEditor.TextArea.PreviewKeyDown += value; }
            remove { textEditor.TextArea.PreviewKeyDown -= value; }
        }

        public event EventHandler SelectionChanged
        {
            add { textEditor.TextArea.SelectionChanged += value; }
            remove { textEditor.TextArea.SelectionChanged -= value; }
        }

        public void Select(int selectionStart, int selectionLength)
        {
            textEditor.Select(selectionStart, selectionLength);
            textEditor.TextArea.Caret.BringCaretToView();
        }

        public void JumpTo(int line, int column)
        {
            //textEditor.TextArea.ClearSelection();

            textEditor.TextArea.Caret.Position = new TextViewPosition(line, column);
            // might have jumped to a different location if column was outside the valid range
            TextLocation actualLocation = textEditor.TextArea.Caret.Location;
            if (textEditor.ActualHeight > 0)
            {
                textEditor.ScrollTo(actualLocation.Line, actualLocation.Column);
            }
            else
            {
                // we have to delay the scrolling if the text editor is not yet loaded
                textEditor.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(
                                                                                 () =>
                                                                                 textEditor.ScrollTo(
                                                                                     actualLocation.Line,
                                                                                     actualLocation.Column)));
            }
        }

        public string FileName { get; set; }

        public virtual IInsightWindow ActiveInsightWindow
        {
            get
            {
                return null;
            }
        }

        public virtual IInsightWindow ShowInsightWindow(IEnumerable<IInsightItem> items)
        {
            return null;
        }

        public virtual ICompletionListWindow ActiveCompletionWindow
        {
            get
            {
                return null;
            }
        }

        public virtual ICompletionListWindow ShowCompletionWindow(ICompletionItemList data)
        {
            return null;
        }

        public virtual IEnumerable<ICompletionItem> GetSnippets()
        {
            return Enumerable.Empty<ICompletionItem>();
        }

        public virtual ITextEditor PrimaryView
        {
            get
            {
                return this;
            }
        }
    }

    
}