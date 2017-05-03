using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Sanford.Multimedia.Midi;

namespace KinHarpe
{
    /// <summary>
    /// Interaction logic for TestMidi.xaml
    /// </summary>
    public partial class TestMidi : Window
    {
        public TestMidi()
        {
            InitializeComponent();
        }

        OutputDevice midiSynth = null;
        int midiChannel = 0;
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (OutputDevice.DeviceCount < 1)
            {
                MessageBox.Show("No midi output device", "No midi", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                Close();
            }
            midiSynth = new OutputDevice(0);    // on prend le premier périphérique de sortie Midi

            foreach (var r in Enum.GetValues(typeof(Sanford.Multimedia.Midi.GeneralMidiInstrument)))
            {
                cbxInstrument.Items.Add(r);
            }
            cbxInstrument.SelectedItem = GeneralMidiInstrument.ChoirAahs;
            
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (midiSynth != null)
            {
                midiSynth.Close();
            }
        }

        private void rectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (midiSynth == null) return;
            FrameworkElement fe = sender as FrameworkElement;
            if (fe==null) return;
            ChannelMessageBuilder builder = new ChannelMessageBuilder();
            builder.Command = ChannelCommand.NoteOn;
            builder.MidiChannel = midiChannel;
            builder.Data1 = Convert.ToInt32(fe.Tag);
            builder.Data2 = 127;
            builder.Build();
            midiSynth.Send(builder.Result);
        }

        private void rectangle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (midiSynth == null) return;
            FrameworkElement fe = sender as FrameworkElement;
            if (fe == null) return;
            ChannelMessageBuilder builder = new ChannelMessageBuilder();
            builder.Command = ChannelCommand.NoteOff;
            builder.MidiChannel = midiChannel;
            builder.Data1 = Convert.ToInt32(fe.Tag);
            builder.Data2 = 0;
            builder.Build();
            midiSynth.Send(builder.Result);
        }

        private void cbxInstrument_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (midiSynth == null) return;
            ChannelMessageBuilder builder = new ChannelMessageBuilder();
            builder.Command = ChannelCommand.ProgramChange;
            builder.MidiChannel = midiChannel;
            builder.Data1 = Convert.ToInt32(cbxInstrument.SelectedItem); // INSTRUMENT ID
            builder.Data2 = 0;
            builder.Build();
            midiSynth.Send(builder.Result);
        }

    }
}
