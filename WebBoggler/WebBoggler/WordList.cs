using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace WebBogglerCommonTypes
{
	public partial class WordList : ObservableCollection <Word>
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
