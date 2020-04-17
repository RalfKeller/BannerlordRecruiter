using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace Recruiter
{
    class RecruiterProperties
    {
        [SaveableField(1)]
        private MobileParty _party;

        [SaveableField(2)]
        private CultureObject _searchCulture;
        public MobileParty party {
            get { return _party; } set { _party = value; } }
        public CultureObject SearchCulture { get { return _searchCulture; } set { _searchCulture = value; } }
    }
}
