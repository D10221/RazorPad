using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using NLog;
using RazorPad.Framework;
using RazorPad.Persistence;
using RazorPad.UI;
using RazorPad.UI.ModelBuilders;
using RazorPad.UI.Persistence;
using RazorPad.UI.Settings;
using RazorPad.UI.Theming;

namespace RazorPad.ViewModels
{
    [Export]
    public class MainViewModel : ViewModelBase
    {
        protected const double DefaultFontSize = 12;
        protected static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly RazorDocumentManager _documentManager;
        private readonly ModelProviders _modelProviders;
        private readonly ModelBuilders _modelBuilders;

        public event EventHandler<EventArgs<string>> Error;

        public ICommand AnchorableCloseCommand { get; private set; }
        public ICommand CloseCommand { get; private set; }
        public ICommand ExecuteCommand { get; private set; }
        public ICommand FontSizeCommand { get; private set; }
        public ICommand ManageReferencesCommand { get; private set; }
        public ICommand NewCommand { get; private set; }
        public ICommand OpenCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }
        public ICommand SaveAsCommand { get; private set; }
        public ICommand SwitchThemeCommand { get; private set; }

        // Use thunks to create test seams
        public Func<RazorTemplateViewModel, MessageBoxResult> ConfirmSaveDirtyDocumentThunk =
            MessageBoxHelpers.ShowConfirmSaveDirtyDocumentMessageBox;

        public Func<string> GetOpenFilenameThunk =
            MessageBoxHelpers.ShowOpenFileDialog;

        public Func<IEnumerable<string>, IEnumerable<string>> GetReferencesThunk =
            references => references;

        public Func<RazorTemplateViewModel, string> GetSaveAsFilenameThunk =
            MessageBoxHelpers.ShowSaveAsDialog;

        public Action<string> ShowErrorThunk =
            MessageBoxHelpers.ShowErrorMessageBox;

        public Action<string> LoadThemeFromFileThunk =
            filename =>
            {
                using (var stream = File.OpenRead(filename))
                {
                    var dic = (ResourceDictionary)XamlReader.Load(stream);
                    Application.Current.Resources.MergedDictionaries.Clear();
                    Application.Current.Resources.MergedDictionaries.Add(dic);
                }
            };


        public RazorTemplateViewModel CurrentTemplate
        {
            get { return _currentTemplate; }
            set
            {
                if (_currentTemplate == value)
                    return;

                _currentTemplate = value;
                Log.Debug("CurrentTemplate changed");
                OnPropertyChanged("CurrentTemplate");
                OnPropertyChanged("HasCurrentTemplate");
            }
        }
        private RazorTemplateViewModel _currentTemplate;

        public bool HasCurrentTemplate
        {
            get { return CurrentTemplate != null; }
        }

        public ObservableCollection<RazorTemplateViewModel> Templates
        {
            get;
            private set;
        }

        public double FontSize
        {
            get { return Preferences.FontSize.GetValueOrDefault(DefaultFontSize); }
            set
            {
                if (Preferences.FontSize == value)
                    return;

                Preferences.FontSize = value;
                OnPropertyChanged("FontSize");
            }
        }

        public bool AutoExecute
        {
            get { return Preferences.AutoExecute.GetValueOrDefault(true); }
            set
            {
                if (Preferences.AutoExecute == value)
                    return;

                Preferences.AutoExecute = value;

                if (Templates != null)
                {
                    foreach (var editor in Templates)
                    {
                        editor.AutoExecute = value;
                    }
                }

                OnPropertyChanged("AutoExecute");
            }
        }

        public bool AutoSave
        {
            get { return Preferences.AutoSave.GetValueOrDefault(true); }
            set
            {
                if (Preferences.AutoSave == value)
                    return;

                Preferences.AutoSave = value;

                if (Templates != null)
                {
                    foreach (var editor in Templates)
                    {
                        editor.AutoSave = value;
                    }
                }

                OnPropertyChanged("AutoSave");
            }
        }

        public string StatusMessage
        {
            get { return _statusMessage; }
            set
            {
                if (_statusMessage == value)
                    return;

                _statusMessage = value;
                OnPropertyChanged("StatusMessage");
            }
        }
        private string _statusMessage;

        public ObservableTextWriter Messages { get; set; }

        public ObservableCollection<string> RecentFiles { get; set; }

        public ObservableCollection<Theme> Themes
        {
            get { return _themes; }
            set
            {
                if (_themes == value)
                    return;
                _themes = value;
                OnPropertyChanged("Themes");
            }
        }
        private ObservableCollection<Theme> _themes;

        [Import(AllowDefault = true)]
        public IRazorDocumentLocator Locator { get; set; }

        [Import(AllowDefault = true)]
        public AutoSaver AutoSaver { get; set; }

        [Import]
        public Preferences Preferences { get; set; }


        [ImportingConstructor]
        public MainViewModel(RazorDocumentManager documentManager, ModelProviders modelProviders, ModelBuilders modelBuilders)
        {
            _documentManager = documentManager;
            _modelBuilders = modelBuilders;
            _modelProviders = modelProviders;

            Templates = new ObservableCollection<RazorTemplateViewModel>();

            RegisterCommands();
        }


        private void RegisterCommands()
        {
            AnchorableCloseCommand = new RelayCommand(
                () => { /* Ignore */ },
                () => false);

            ExecuteCommand = new RelayCommand(
                    p => CurrentTemplate.Execute(),
                    p => CurrentTemplate != null
                );

            CloseCommand = new RelayCommand(
                    p => Close(CurrentTemplate),
                    p => HasCurrentTemplate
                );

            FontSizeCommand = new RelayCommand(ChangeFontSize);

            ManageReferencesCommand = new RelayCommand(() =>
            {
                var loadedReferences = CurrentTemplate.AssemblyReferences;
                CurrentTemplate.AssemblyReferences = GetReferencesThunk(loadedReferences);
            });

            NewCommand = new RelayCommand(() => AddNewTemplateEditor());

            OpenCommand = new RelayCommand(p =>
            {
                string filename = p as string;

                if (string.IsNullOrWhiteSpace(filename))
                {
                    if (Locator == null)
                        filename = GetOpenFilenameThunk();
                    else
                        filename = Locator.Locate();
                }

                if (string.IsNullOrWhiteSpace(filename))
                    return;

                AddNewTemplateEditor(filename);
            });

            SaveCommand = new RelayCommand(
                    p => Save(CurrentTemplate),
                    p => HasCurrentTemplate
                );

            SaveAsCommand = new RelayCommand(
                    p => SaveAs(CurrentTemplate),
                    p => HasCurrentTemplate && CurrentTemplate.CanSaveAsNewFilename
                );

            SwitchThemeCommand = new RelayCommand(
                    p => SwitchTheme((Theme)p),
                    p => true
                );
        }

        private void ChangeFontSize(object param)
        {
            var strVal = param.ToString();

            if (strVal == "Increase")
                FontSize += 2;
            if (strVal == "Decrease")
                FontSize -= 2;
            if (strVal == "Reset")
                FontSize = DefaultFontSize;

            double size;
            if (double.TryParse(strVal, out size))
            {
                FontSize = size;
            }
        }

        private void SwitchTheme(Theme theme)
        {
            Log.Debug("Switching to {0} theme ({1})...", theme.Name, theme.FilePath);

            LoadThemeFromFileThunk(theme.FilePath);
            Themes.ToList().ForEach(x => x.Selected = false);
            theme.Selected = true;

            Preferences.Theme = theme.Name;

            Log.Info("Switched to {0} theme", theme.Name);
        }

        public void AddNewTemplateEditor(bool current = true)
        {
            var modelProvider = ModelProviders.DefaultFactory.Create();

            var document = new RazorDocument { ModelProvider = modelProvider };

            AddNewTemplateEditor(document, current);
        }

        public void AddNewTemplateEditor(string filename, bool current = true)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                Log.Info("Attempted to add new editor without specifying a filename -- returning");
                return;
            }

            RazorTemplateViewModel loadedTemplate =
                Templates
                    .Where(x => !string.IsNullOrWhiteSpace(x.Filename))
                    .SingleOrDefault(x => x.Filename.Equals(filename, StringComparison.OrdinalIgnoreCase));

            if (loadedTemplate != null)
            {
                if (current)
                    CurrentTemplate = loadedTemplate;

                return;
            }

            var document = _documentManager.Load(filename);
            document.Filename = filename;

            AddNewTemplateEditor(document, current);
        }

        public void AddNewTemplateEditor(RazorDocument document, bool current = true)
        {
            AddNewTemplateEditor(new RazorTemplateViewModel(document, _modelBuilders, _modelProviders), current);
        }

        public void AddNewTemplateEditor(RazorTemplateViewModel template, bool current = true)
        {
            Log.Debug("Adding new template editor (current: {0})...", current);

            template.AutoExecute = AutoExecute;
            template.AutoSave = AutoExecute;
            template.Messages = Messages;

            template.Executing += OnAutoSave;

            Templates.Add(template);

            if (!string.IsNullOrWhiteSpace(template.Filename))
                RecentFiles.Add(template.Filename);

            if (current)
            {
                Log.Debug("Setting as current template");
                CurrentTemplate = template;
            }

            template.Execute();

            Log.Info("Added new template editor");
        }

        public void Close(RazorTemplateViewModel document, bool? save = null)
        {
            Log.Debug("Closing document...");

            if (document.IsDirty && save.GetValueOrDefault(true))
            {
                Log.Debug("Document is dirty confirming save or close...");

                var shouldSave = ConfirmSaveDirtyDocumentThunk(document);

                Log.Debug("User said {0}", shouldSave);

                switch (shouldSave)
                {
                    case MessageBoxResult.Cancel:
                        return;

                    case MessageBoxResult.Yes:
                        Save(document);
                        break;
                }
            }

            Templates.Remove(document);

            Log.Debug("Document closed");
        }

        public void LoadAutoSave()
        {
            try
            {
                var template = AutoSaver.Load();
                
                if(template != null)
                    AddNewTemplateEditor(template);
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Auto-save file found, but there was an error loading it.");
            }
        }

        public void Save(RazorTemplateViewModel document)
        {
            var filename = document.Filename;

            if (!document.CanSaveToCurrentlyLoadedFile)
                filename = null;

            SaveAs(document, filename);
        }

        public string SaveAs(RazorTemplateViewModel document, string filename = null)
        {
            try
            {
                if (filename == null)
                {
                    Log.Debug("Filename was null -- triggering SaveAs...");
                    filename = GetSaveAsFilenameThunk(document);
                }

                if (string.IsNullOrWhiteSpace(filename))
                {
                    Log.Warn("Filename is empty - skipping save");
                    return filename;
                }

                Log.Debug("Saving document to {0}...", filename);

                _documentManager.Save(document.Document, filename);

                Log.Info("Document saved to {0}", filename);

                document.Filename = filename;

                if (!string.IsNullOrWhiteSpace(filename))
                    RecentFiles.Add(filename);

                if(AutoSaver != null)
                    AutoSaver.Clear();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving document");
                Error.SafeInvoke(ex.Message);
            }

            return filename;
        }

        public void SetRecentReferences(IEnumerable<string> references)
        {
            Preferences.RecentReferences = references;
        }

        private void OnAutoSave(object sender, EventArgs e)
        {
            if(AutoSaver == null)
                return;

            var template = sender as RazorTemplateViewModel ?? CurrentTemplate;

            if (template == null)
                return;

            new TaskFactory().StartNew(() => {
                try
                {
                    AutoSaver.Save(template.Document);
                    Log.Info("Auto-Saved the document for you -- you can thank me later.");
                }
                catch (Exception ex)
                {
                    Log.Warn(ex, "Auto-save failed");
                }
            });
        }
    }
}
