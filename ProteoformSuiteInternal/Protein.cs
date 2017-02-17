using System.Collections.Generic;
using System.Linq;
using Proteomics;

namespace ProteoformSuiteInternal
{
    public class ProteinSequenceGroup : Protein
    {
        public List<string> accessionList { get; set; } // this is the list of accession numbers for all proteins that share the same sequence. the list gets alphabetical order

        public ProteinSequenceGroup(string accession, string name, string fragment, int begin, int end, string sequence, List<GoTerm> goTerms, Dictionary<int, List<Modification>> positionsAndPtms)
            : base(accession, name, fragment, begin, end, sequence, goTerms, positionsAndPtms)
        { }
        public ProteinSequenceGroup(List<Protein> proteins)
            : base(proteins[0].accession + "_G" + proteins.Count(), proteins[0].name, proteins[0].fragment, proteins[0].begin, proteins[0].end, proteins[0].sequence, proteins[0].goTerms, proteins[0].ptms_by_position)
        {
            this.accessionList = proteins.Select(p => p.Accession).ToList();
            HashSet<int> all_positions = new HashSet<int>(proteins.SelectMany(p => p.OneBasedPossibleLocalizedModifications.Keys).ToList());
            Dictionary<int, List<Modification>> ptms_by_position = new Dictionary<int, List<Modification>>();
            foreach (int position in all_positions)
                ptms_by_position.Add(position, proteins.Where(p => p.OneBasedPossibleLocalizedModifications.ContainsKey(position)).SelectMany(p => p.OneBasedPossibleLocalizedModifications[position]).ToList());
        }
    }
}
