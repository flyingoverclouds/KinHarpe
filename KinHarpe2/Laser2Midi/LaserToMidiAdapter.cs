using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sanford.Multimedia.Midi;

namespace KinHarpe2.Laser2Midi
{
    /// <summary>
    /// This class translate a hand motion on laser to midi sequence
    /// </summary>
    class LaserToMidiAdapter
    {
        Dictionary<string, int> laserToMidiTranslation = new Dictionary<string, int>();
        OutputDevice midiSynth = null;
        private int midiChannel = 0;

        private GeneralMidiInstrument currentInstrument;

        public LaserToMidiAdapter(OutputDevice midiDevice, int midiChannel)
        {
            // set the midiChannel
            this.midiChannel = midiChannel;

            // selecte default instrument
            GeneralMidiInstrument currentInstrument = GeneralMidiInstrument.AcousticGrandPiano;

            // defining translation for laser to midi "note"
            laserToMidiTranslation.Add("A", 60);
            laserToMidiTranslation.Add("B", 62);
            laserToMidiTranslation.Add("C", 64);
            laserToMidiTranslation.Add("D", 66);
            laserToMidiTranslation.Add("E", 68);
            laserToMidiTranslation.Add("F", 70);
            laserToMidiTranslation.Add("G", 72);
            laserToMidiTranslation.Add("H", 74);
            laserToMidiTranslation.Add("I", 76);

            midiSynth = midiDevice;

            SetInstrument(currentInstrument); // set the instrument to use 
        }

        public bool DeviceAvailable
        {
            get { return midiSynth != null; }
        }

        public GeneralMidiInstrument CurrentInstrument
        {
            get { return currentInstrument; }
            set
            {
                currentInstrument = value;
                SetInstrument(currentInstrument);
            }
        }

        public int Channel
        {
            get { return midiChannel; }
        }


        private string previousLaser = "";
        public void ReleaseLaser()
        {
            if (previousLaser == string.Empty)
                return;
            string laserToStop = previousLaser;
            previousLaser = "";
            StopNote(laserToStop);
        }

        public void PlayLaser(string laserNote)
        {
            string laserToStop = previousLaser;
            if (previousLaser == laserNote)
                return;
            previousLaser = laserNote; 
            
            // Stop current note
            StopNote(laserToStop);
            
            // Get note to play for selected laser 
            PlayNote(laserNote);
        }

        void SetInstrument(GeneralMidiInstrument gmInstrument)
        {
            if (midiSynth == null) 
                return;
            ChannelMessageBuilder builder = new ChannelMessageBuilder();
            builder.Command = ChannelCommand.ProgramChange;
            builder.MidiChannel = midiChannel;
            builder.Data1 = Convert.ToInt32(gmInstrument); // INSTRUMENT ID
            builder.Data2 = 0;
            builder.Build();
            midiSynth.Send(builder.Result);
        }


        void StopNote(string laserNote)
        {
            if (midiSynth == null || laserNote == string.Empty)
                return;
            Debug.WriteLine("---- " + laserNote);
            ChannelMessageBuilder builder = new ChannelMessageBuilder();
            builder.Command = ChannelCommand.NoteOff;
            builder.MidiChannel = Channel;
            builder.Data1 = laserToMidiTranslation[laserNote];
            builder.Data2 = 0;
            builder.Build();
            midiSynth.Send(builder.Result);
        }


        void PlayNote(string laserNote)
        {
            Debug.WriteLine("PLAY " + laserNote);
            if (midiSynth == null)
                return;
            ChannelMessageBuilder builder = new ChannelMessageBuilder();
            builder.Command = ChannelCommand.NoteOn;
            builder.MidiChannel = Channel;
            builder.Data1 = laserToMidiTranslation[laserNote];
            builder.Data2 = 127;
            builder.Build();
            midiSynth.Send(builder.Result);
        }
    }
}
