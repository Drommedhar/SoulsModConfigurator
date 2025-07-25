using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SoulsConfigurator.Interfaces;

namespace SoulsModConfigurator.Controls
{
    /// <summary>
    /// Interaction logic for ModListCtrl.xaml
    /// </summary>
    public partial class ModListCtrl : UserControl
    {
        private List<IMod> _mods = new List<IMod>();

        // Event to notify parent when mod selection changes
        public event EventHandler? SelectionChanged;

        public ModListCtrl()
        {
            InitializeComponent();
        }

        public void RefreshMods(List<IMod> mods)
        {
            _mods = mods ?? new List<IMod>();
            
            // Clear existing mod entries
            var stackPanel = FindChild<StackPanel>(this);
            if (stackPanel != null)
            {
                stackPanel.Children.Clear();
                
                // Add mod entries dynamically
                foreach (var mod in _mods)
                {
                    var modEntry = new ModEntryCtrl();
                    modEntry.Initialize(mod);
                    
                    // Subscribe to selection change events
                    modEntry.SelectionChanged += OnModSelectionChanged;
                    
                    stackPanel.Children.Add(modEntry);
                }
            }
        }

        private void OnModSelectionChanged(object? sender, EventArgs e)
        {
            // Notify parent that selection has changed
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        public List<IMod> GetSelectedMods()
        {
            var selectedMods = new List<IMod>();
            var stackPanel = FindChild<StackPanel>(this);
            
            if (stackPanel != null)
            {
                foreach (var child in stackPanel.Children.OfType<ModEntryCtrl>())
                {
                    if (child.IsModSelected)
                    {
                        var mod = child.GetMod();
                        if (mod != null)
                        {
                            selectedMods.Add(mod);
                        }
                    }
                }
            }
            
            return selectedMods;
        }

        // Helper method to find child controls
        private T? FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;

                var childOfChild = FindChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }
    }
}
