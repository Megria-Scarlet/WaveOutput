using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MegriaCore.YMM4.WaveOutput
{
    public partial class OptionControl : UserControl
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public OptionControl()
        {
            InitializeComponent();
        }

        private readonly static KeyValuePair<WaveBits, string>[] bitsPairs = [
            new KeyValuePair<WaveBits, string>(WaveBits.PCM16, "16 bit"),
            new KeyValuePair<WaveBits, string>(WaveBits.PCM24, "24 bit"),
            new KeyValuePair<WaveBits, string>(WaveBits.Float, "Float")
            ];
        private readonly static System.Collections.ObjectModel.ReadOnlyCollection<KeyValuePair<WaveBits, string>> bitsPairList = new(bitsPairs);
        public static System.Collections.ObjectModel.ReadOnlyCollection<KeyValuePair<WaveBits, string>> BitsPairList => bitsPairList;
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
