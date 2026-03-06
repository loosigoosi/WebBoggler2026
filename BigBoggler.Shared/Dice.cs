using System;

namespace BigBoggler.Models
{
    public class Dice
    {
        private readonly string[] _faces = new string[6];
        private readonly Random _rng = new Random();
        public int Row { get; set; }
        public int Column { get; set; }
        public int SelectedFace { get; set; }
        public int FaceRotation { get; set; } // 0, 90, 180, 270

        public Dice(string[] faces) { Array.Copy(faces, _faces, 6); }
        public string SelectedString => _faces[SelectedFace];
        public void Randomize()
        {
            SelectedFace = _rng.Next(0, 6);
            FaceRotation = _rng.Next(0, 4) * 90;
        }
        public override string ToString() => SelectedString;
    }
}