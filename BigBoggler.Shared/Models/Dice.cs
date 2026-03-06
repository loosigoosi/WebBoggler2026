using System;
using System.Runtime.Serialization;

namespace BigBoggler.Models
{
    [DataContract]
    public class Dice
    {
        private readonly string[] _faces = new string[6];
        private readonly Random _rng = new Random();
        
        [DataMember]
        public int Row { get; set; }
        
        [DataMember]
        public int Column { get; set; }
        
        [DataMember]
        public int SelectedFace { get; set; }
        
        [DataMember]
        public int FaceRotation { get; set; } // 0, 90, 180, 270

        // Proprietà calcolate (non serializzate)
        public string SelectedString => _faces[SelectedFace];
        public int Index => Row * 5 + Column;
        
        // Per serializzazione SignalR
        [DataMember]
        public string Letter 
        { 
            get => SelectedString; 
            set { /* Ignorato in deserializzazione */ } 
        }
        
        [DataMember]
        public int Rotation 
        { 
            get => FaceRotation; 
            set => FaceRotation = value; 
        }

        public Dice() 
        { 
            _faces = new string[6]; 
        }

        public Dice(string[] faces) 
        { 
            _faces = new string[6];
            Array.Copy(faces, _faces, 6); 
        }

        public void Randomize()
        {
            SelectedFace = _rng.Next(0, 6);
            FaceRotation = _rng.Next(0, 4) * 90;
        }
        
        public override string ToString() => SelectedString;
    }
}