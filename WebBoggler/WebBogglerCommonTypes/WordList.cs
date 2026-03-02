using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;


namespace WebBogglerCommonTypes
{

	[DataContract(IsReference = true, Namespace = "WebBogglerCommonTypes")]
	public class WordList : List<WordBase>
    {

		public int GetTotalScore(bool includeDuplicates = false)
        {

            int score = 0;
            if (includeDuplicates)
            {
                score += this.Sum(pair => pair.Score);
            }
            else
            {
                score += this.Where(pair => pair.Duplicated == false).Sum(pair => pair.Score);
            }
            return score;

        }

 
    }
}
