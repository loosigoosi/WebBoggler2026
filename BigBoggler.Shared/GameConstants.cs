using System.Security.Claims;

namespace BigBoggler.Models
{

    // Vecchia classe per WP8, ora convertita in una classe statica per contenere le costanti del gioco
    // <copyright company = "Exit Games GmbH" > ' Exit Games GmbH, 2012' </ copyright > ' <summary>' The "Particle" demo is a load balanced and Photon Cloud compatible "coding" demo.' The focus is on showing how to use the Photon features without too much "game" code cluttering the view.' </summary>' <author>developer@exitgames.com</author>' --------------------------------------------------------------------------------------------------------------------''' <summary>''' Class to define a few constants used in this demo (for event codes, properties, etc).''' </summary>''' <remarks>''' These values are something made up for this particular demo! ''' You can define other values (and more) in your games, as needed.''' </remarks>Public NotInheritable Class WPConstants Private Sub New() End Sub Public Const LanguageProp As String = "la" Public Enum Language Italiano English End Enum Public Const MatchTypeProp As String = "mt" Public Enum MatchType Duel Rolling End Enum Public Const VisibilityProp As String = "v" Public Enum Visibility PrivateRoom PublicRoom End Enum ''' <summary>(1) Event defining a color of a player.</summary> Public Const EvColor As Byte = 1 ''' <summary>(2) Event defining the position of a player.</summary> 
    public static class GameConstants
    {
        // Proprietà della stanza (ex WPConstants)
        public const string LanguageProp = "la";
        public const string MatchTypeProp = "mt";
        public const string VisibilityProp = "v";

        public enum Language { Italiano, English }
        public enum MatchType { Duel, Rolling }
        public enum Visibility { PrivateRoom, PublicRoom }
    }
}