using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MegriaCore.YMM4.WaveOutput
{
    public partial class WaveOptionControl : UserControl
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public WaveOptionControl()
        {
            InitializeComponent();
        }
        public WaveOptionControl(OutputOption option)
        {
            InitializeComponent();
            this.DataContext = option;
        }

        private readonly static KeyValuePair<WaveBits, string>[] bitsPairs = [
            new KeyValuePair<WaveBits, string>(WaveBits.PCM16, "16 bit"),
            new KeyValuePair<WaveBits, string>(WaveBits.PCM24, "24 bit"),
            new KeyValuePair<WaveBits, string>(WaveBits.Float, "Float")
            ];
        private readonly static System.Collections.ObjectModel.ReadOnlyCollection<KeyValuePair<WaveBits, string>> bitsPairList = new(bitsPairs);
        public static System.Collections.ObjectModel.ReadOnlyCollection<KeyValuePair<WaveBits, string>> BitsPairList => bitsPairList;

        private void PresetReloadButton_Click(object sender, RoutedEventArgs e)
        {
            OutputOption outputOption = (OutputOption)DataContext;
            outputOption.ReloadSamplePresetFile();

            if (sampleCombo.SelectedIndex < 0)
                sampleCombo.SelectedIndex = 0;
            if (bpsCombo.SelectedIndex < 0)
                bpsCombo.SelectedIndex = 0;
        }

        private void PresetEditButton_Click(object sender, RoutedEventArgs e)
        {
            OutputOption outputOption = (OutputOption)DataContext;
            outputOption.OpenSamplePresetFile();
        }
    }
    public class PersonTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            ContentPresenter presenter = (ContentPresenter)container;
            if (presenter.TemplatedParent is ComboBox)
                return (DataTemplate)presenter.FindResource("ComboView");
            else
                return (DataTemplate)presenter.FindResource("ComboSelector");
        }
    }
}
