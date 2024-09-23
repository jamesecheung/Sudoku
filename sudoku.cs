using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Formats.Asn1;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

/* DEFINITIONS: 
block: 3x3 square that cell is in
house: row + column + block
puzzle: the initial puzzle (string with the zeroes)
sudoku: the array with candidates (to be actually worked on by the algorithms)
PencilInNumbers method converts puzzle to sudoku by filling in all candidates
 */

class Sudoku
{
    /* HELPER FUNCTIONS 
    #########################################################################  */
    // add candidates and already-filled-in answers from puzzle array to cells in sudoku array
    static object[,] PencilInNumbers(int[,] puzzle)
    {
        object[,] sudoku = new object[9, 9];
        for (int j = 0; j < 9; j++)
        {
            for (int i = 0; i < 9; i++)
            {
                // if cell in original puzzle already filled in
                if (puzzle[i, j] != 0)
                {
                    // add answer as the sole candidate in the cell
                    sudoku[i, j] = new List<int>() {puzzle[i, j]};
                }
                // not filled in
                else
                {
                    // add candidates from 1 to 9
                    sudoku[i, j] = new List<int>(); 
                    for (int num = 1; num < 10; num++)
                    {
                        ((List<int>)sudoku[i, j]).Add(num);
                    }
                }
            }
        }
        return sudoku;
    }

    // count no. of solved cells
    static int CountSolved(object[,] sudoku)
    {
        int solved = 0;

        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                if (sudoku[i, j] is List<int> candidate && candidate.Count == 1)
                {
                    solved++;
                }
            }
        }

        return solved;
    }

    // count no. of remaining unsolved candidates to remove
    static int CountToRemove(object[,] sudoku)
    {
        int toRemove = 0;

        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                if (sudoku[i, j] is List<int> candidates)
                {
                    toRemove += candidates.Count - 1;
                }
            }   
        }

        return toRemove;
    }

    // count no. of solved cells in a group
    static int CountSolvedInGroup(List<(int,int)> group, object[,] sudoku)
    {
        int solved = 0;
        
        foreach (var cell in group)
        {
            if (sudoku[cell.Item1, cell.Item2] is List<int> candidate && candidate.Count == 1)
            {
                solved++;
            }
        }

        return solved;
    }

    // converts puzzle string into internally useable puzzle array
    static int[,] ConvertToPuzzleArray(string line)
    {
        if (line.Length < 81)
        {
            throw new ArgumentException("Input string must have at least 81 characters.", nameof(line));
        }

        string raw_s = line.Substring(0, 81);

        if (!raw_s.All(char.IsDigit))
        {
            throw new ArgumentException("Input string must contain only digits.", nameof(line));
        }

        //  convert the string to an array of integers
        int[] s_np1 = raw_s.Select(c => c - '0').ToArray();

        int[,] s_np = new int[9, 9];

        for (int i = 0; i < s_np1.Length; i++)
        {
            int row = i / 9;
            int col = i % 9;
            s_np[row, col] = s_np1[i];
        }

        return s_np;
    }

    // converts sudoku array back into puzzle string
    static string ConvertToString(object[,] sudoku)
    {
        int[,] result = new int[9, 9];

        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                // Extract the single integer value from the list
                result[i, j] = ((List<int>)sudoku[i, j])[0];
            }

        }
        
        StringBuilder sb = new StringBuilder();

        //  append each integer
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                sb.Append(result[i, j]);
            }
        }

        return sb.ToString();
    }

    // get all candidates in the group
    static List<int> GetAllUniqueCandidates(List<(int, int)> group, object[,] sudoku)
    {
        List<int> allCandidates = group
                .Select(cell => sudoku[cell.Item1, cell.Item2])
                .OfType<List<int>>()
                .SelectMany(candidates => candidates)
                .Distinct()
                .ToList();

        return allCandidates;
    }

    // get candidate frequencies in a group
    static Dictionary<int, int> GetCandidateFrequencies(List<(int, int)> group, object[,] sudoku)
    {
        List<int> candidatesInCell = group
                .Select(cell => sudoku[cell.Item1, cell.Item2])
                .OfType<List<int>>()
                .SelectMany(candidates => candidates)
                .ToList();
        Dictionary<int, int> candidateFreq = new Dictionary<int, int>();
        foreach (var candidate in candidatesInCell)
        {
            if (!candidateFreq.ContainsKey(candidate))
            {
                candidateFreq[candidate] = 0;
            }
            candidateFreq[candidate]++;
        }

        return candidateFreq;
    }

    // generates all houses (rows, columns, blocks) for iteration later on
    static List<List<(int, int)>> GenerateAllRows()
    {
        List<List<(int, int)>> allRows = new List<List<(int, int)>>();
        for (int i = 0; i < 9; i++)
        {
            //  add rows
            List<(int, int)> row = new List<(int, int)>();
            for (int j = 0; j < 9; j++)
            {
                row.Add((i, j));
            }
            allRows.Add(row);
        }
        return allRows;
    }
    static List<List<(int, int)>> GenerateAllColumns()
    {
        List<List<(int, int)>> allColumns = new List<List<(int, int)>>();
        for (int i = 0; i < 9; i++)
        {
            //  add columns
            List<(int, int)> column = new List<(int, int)>();
            for (int j = 0; j < 9; j++)
            {
                column.Add((j, i));
            }
            allColumns.Add(column);
        }
        return allColumns;
    }
    static List<List<(int, int)>> GenerateAllBlocks()
    {
        List<List<(int, int)>> allBlocks = new List<List<(int, int)>>();
        for (int i = 0; i < 9; i++)
        {
            //  add blocks
            List<(int, int)> block = new List<(int, int)>();
            int blockRowStart = (i / 3) * 3;
            int blockColStart = (i % 3) * 3;
            for (int j = blockRowStart; j < blockRowStart + 3; j++)
            {
                for (int k = blockColStart; k < blockColStart + 3; k++)
                {
                    block.Add((j, k));
                }
            }
            allBlocks.Add(block);
        }
        return allBlocks;
    }
    static List<List<(int, int)>> GenerateAllLines()
    {
        List<List<(int, int)>> allLines = new List<List<(int, int)>>();

        List<List<(int, int)>> allRows = GenerateAllRows();
        foreach (var row in allRows)
        {
            allLines.Add(row);
        }
        List<List<(int, int)>> allColumns = GenerateAllColumns();
        foreach (var column in allColumns)
        {
            allLines.Add(column);
        }

        return allLines;
    }

    static List<List<(int, int)>> GenerateAllHouses()
    {
        List<List<(int, int)>> allHouses = new List<List<(int, int)>>();
        
        List<List<(int, int)>> allRows = GenerateAllRows();
        foreach (var row in allRows)
        {
            allHouses.Add(row);
        }
        List<List<(int, int)>> allColumns = GenerateAllColumns();
        foreach (var column in allColumns)
        {
            allHouses.Add(column);
        }
        List<List<(int, int)>> allBlocks = GenerateAllBlocks();
        foreach (var block in allBlocks)
        {
            allHouses.Add(block);
        }

        return allHouses;
    }

    // gets the row, column and block a given cell belongs to
    static List<List<(int, int)>> GetRowColBlock((int, int) cell, List<List<(int, int)>> allHouses)
    {
        List<List<(int, int)>> rowColBlock = new List<List<(int, int)>>();

        foreach (var group in allHouses)
        {
            if (group.Contains(cell))
            {
                rowColBlock.Add(group);
            }
        }

        return rowColBlock;
    }

    /* SOLVING ALGORITHMS
    #########################################################################  */
    // 0. simple elimination - if there is only one number in cell, remove it from all houses
    static int SimpleElimination(object[,] sudoku, List<List<(int, int)>> allHouses)
    {
        int count = 0;
        
        foreach (var group in allHouses)
        {
            foreach (var cell in group)
            {
                /* 1. type matching expression - is the value stored in the sudoku grid at the specific cell coordinates a list?
                name this list 'candidates' as well
                2. is the number of candidates inside the list exactly 1? 
                this will be the candidate that is checked against the other cells in the group */
                if (sudoku[cell.Item1, cell.Item2] is List<int> candidates && candidates.Count == 1)
                {
                    int candidate = candidates[0];
                    foreach (var cell2 in group)
                    {
                        // similar to above
                        if (cell != cell2 && sudoku[cell2.Item1, cell2.Item2] is List<int> otherCandidates && otherCandidates.Contains(candidate))
                        {
                            otherCandidates.Remove(candidate);
                            count++;
                        }
                    }
                }
            }
        }

        return count;
    }

    // 1. hidden single - if there is only one instance of N in house, keep only it
    static int HiddenSingles(object[,] sudoku, List<List<(int, int)>> allHouses)
    {
        int count = 0;
        foreach(var group in allHouses)
        {
            Dictionary<int, int> candidateFreq = GetCandidateFrequencies(group, sudoku);
            foreach (KeyValuePair<int, int> pair in candidateFreq)
            {
                if (pair.Value == 1)
                {
                    // find the cell containing that one candidate
                    foreach (var cell in group)
                    {
                        if (sudoku[cell.Item1, cell.Item2] is List<int> candidates && candidates.Count > 1 && candidates.Contains(pair.Key))
                        {
                            count += candidates.Count - 1;
                            candidates.Clear();
                            candidates.Add(pair.Key);
                            break;
                        }
                    }
                }
            }
        }

        return count;
    }

    // 2. naked pairs
    static int NakedPairs(object[,] sudoku, List<List<(int, int)>> allHouses)
    {
        int count = 0;

        foreach (var group in allHouses)
        {
            /* dictionary to store occurrences of candidate pairs
            string - key containing the 2 numbers that form the pair
            list - list of cells within the group that contain just that pair */
            Dictionary<string, List<(int, int)>> pairOccurrences = new Dictionary<string, List<(int, int)>>();

            // record occurrences of candidate pairs within the group
            foreach (var cell in group)
            {
                if (sudoku[cell.Item1, cell.Item2] is List<int> candidates && candidates.Count == 2)
                {
                    // form the string that will make up the key in dict
                    string candidateKey = string.Join(",", candidates);
                    
                    // does the dict not already contain this particular string key?
                    if (!pairOccurrences.ContainsKey(candidateKey))
                    {
                        // add this string key into dict, with an associated list value
                        pairOccurrences[candidateKey] = new List<(int, int)>();
                    }
                    pairOccurrences[candidateKey].Add(cell);
                }
            }

            // eliminate candidates based on naked pairs
            foreach (var stringKey in pairOccurrences.Keys)
            {
                /* if exactly 2 cells have the same pair of candidates, 
                eliminate these candidates from other cells in the group */
                if (pairOccurrences[stringKey].Count == 2)
                {
                    foreach (var cell in group)
                    {
                        if (pairOccurrences[stringKey].Contains(cell))
                        {
                            continue; // skip cells that are part of the naked pair
                        }

                        if (sudoku[cell.Item1, cell.Item2] is List<int> cellCandidates)
                        {
                            string[] parts = stringKey.Split(',');
                            
                            int candidate1 = int.Parse(parts[0]);
                            if (cellCandidates.Contains(candidate1))
                            {
                                cellCandidates.Remove(candidate1);
                                count++;
                            }

                            int candidate2 = int.Parse(parts[1]);
                            if (cellCandidates.Contains(candidate2))
                            {
                                cellCandidates.Remove(candidate2);
                                count++;
                            }
                        }
                    }
                }
            }
        }

        return count;
    }

    // 3. naked triples
    // get all possible permutations of triples out of a given list of candidates
    static List<List<int>> GetCandidateTriples(List<int> candidates)
    {
        List<List<int>> triples = new List<List<int>>();

        for (int i = 0; i < candidates.Count - 2; i++)
        {
            for (int j = i + 1; j < candidates.Count - 1; j++)
            {
                for (int k = j + 1; k < candidates.Count; k++)
                {
                    List<int> triple = new List<int> { candidates[i], candidates[j], candidates[k] };
                    triples.Add(triple);
                }
            }
        }

        return triples;
    }

    static int NakedTriples(object[,] sudoku, List<List<(int, int)>> allHouses)
    {
        int count = 0;

        foreach (var group in allHouses)
        {
            List<int> tripleCandidates = new List<int>();
            foreach (var cell in group)
            {
                if (sudoku[cell.Item1, cell.Item2] is List<int> candidates)
                {
                    if (candidates.Count == 2 || candidates.Count == 3)
                    {
                        foreach (var candidate in candidates)
                        {
                            if (!tripleCandidates.Contains(candidate))
                            {
                                tripleCandidates.Add(candidate);
                            }
                        }
                    }
                }
            }

            List<List<int>> allCandidateTriples = GetCandidateTriples(tripleCandidates);
            
            Dictionary<string, List<(int, int)>> tripleOccurences = new Dictionary<string, List<(int, int)>>();
            foreach (var candidateTriple in allCandidateTriples)
            {
                List<(int, int)> tripleCells = new List<(int, int)>();

                foreach (var cell in group)
                {
                    if (sudoku[cell.Item1, cell.Item2] is List<int> candidates)
                    {
                        if (candidates.Count == 2)
                        {
                            if (candidates.Intersect(candidateTriple).Count() >= 2)
                            {
                                tripleCells.Add(cell);
                            }
                        } 
                        else if (candidates.Count == 3)
                        {
                            if (candidates.All(candidateTriple.Contains))
                            {
                                tripleCells.Add(cell);
                            }
                        }
                    }
                }

                if (tripleCells.Count == 3)
                {
                    string candidateKey = string.Join(",", candidateTriple);
                    tripleOccurences[candidateKey] = tripleCells;
                }
            }

            foreach (KeyValuePair<string, List<(int, int)>> pair in tripleOccurences)
            {        
                string[] candidateStrings = pair.Key.Split(',');
                List<int> candidateNumbers = candidateStrings.Select(int.Parse).ToList();

                if (pair.Value.Count == 3)
                {
                    foreach (var cell in group)
                    {
                        if (!pair.Value.Contains(cell))
                        {
                            if (sudoku[cell.Item1, cell.Item2] is List<int> cellCandidates)
                            {
                                foreach (int candidate in candidateNumbers)
                                {
                                    if (cellCandidates.Contains(candidate))
                                    {
                                        cellCandidates.Remove(candidate);
                                        count++;
                                    }
                                }
                            }
                        }
                    }
                }
            }

        }
        return count;
    }

    // 4. hidden pairs
    // get all possible permutations of triples out of a given list of candidates
    static List<List<int>> GetCandidatePairs(List<int> candidates)
    {
        List<List<int>> pairs = new List<List<int>>();

        for (int i = 0; i < candidates.Count - 1; i++)
        {
            for (int j = i + 1; j < candidates.Count; j++)
            {
                List<int> pair = new List<int> { candidates[i], candidates[j] };
                pairs.Add(pair);
                
            }
        }

        return pairs;
    }

    static int HiddenPairs(object[,] sudoku, List<List<(int, int)>> allHouses)
    {
        int count = 0;
        foreach (var group in allHouses)
        {
            List<int> allCandidates = GetAllUniqueCandidates(group, sudoku);
            List<List<int>> allPairs = GetCandidatePairs(allCandidates);

            Dictionary<string, List<(int, int)>> cellPairs = new Dictionary<string, List<(int, int)>>();
            foreach (var pair in allPairs)
            {
                foreach (var cell in group)
                {
                    if (sudoku[cell.Item1, cell.Item2] is  List<int> candidates && candidates.Intersect(pair).Count() == 2)
                    {
                        string candidatePair = string.Join(",", pair);
                        if (!cellPairs.ContainsKey(candidatePair))
                        {
                            cellPairs[candidatePair] = new List<(int, int)>();
                        }
                        cellPairs[candidatePair].Add(cell);
                    }
                }
            }
            
            foreach (KeyValuePair<string, List<(int,int)>> pair in cellPairs)
            {
                if (pair.Value.Count == 2)
                {
                    string[] pairOfCandidates = pair.Key.Split(',');
                    List<int> twoNumbers = pairOfCandidates.Select(int.Parse).ToList();

                    bool isValidPair = true;
                    foreach (var cell in group)
                    {
                        if (!pair.Value.Contains(cell))
                        {
                            foreach (int number in twoNumbers)
                            {
                                if (sudoku[cell.Item1, cell.Item2] is List<int> candidates && candidates.Contains(number))
                                {
                                    isValidPair = false;
                                    break;
                                }
                            }
                        }
                    }

                    if(isValidPair)
                    {
                        foreach (var cell in pair.Value)
                        {
                            if (sudoku[cell.Item1, cell.Item2] is List<int> candidates)
                            {
                                List<int> toRemove = candidates.Except(twoNumbers).ToList();
                                count += toRemove.Count;
                                foreach (var candidate in toRemove)
                                {
                                    candidates.Remove(candidate);
                                }
                            }
                        }
                    }
                }
            }
        }

        return count;
    }

    // 5. hidden triples
    // from the candidate freq dictionary, return only the ones that occur at most thrice
    static int HiddenTriples(object[,] sudoku, List<List<(int, int)>> allHouses)
    {
        int count = 0;

        foreach (var group in allHouses)
        {
            if (CountSolvedInGroup(group, sudoku) > 2)
            {
                continue;
            }
            
            Dictionary<int,int> candidateFreq = new Dictionary<int,int>();
            candidateFreq = GetCandidateFrequencies(group, sudoku);
            
            List<int> suitableCandidates = new List<int>();
            foreach (KeyValuePair<int,int> pair in candidateFreq)
            {
                if (pair.Value == 2 || pair.Value == 3)
                {
                    suitableCandidates.Add(pair.Key);
                }
            }

            List<List<int>> allTriples = GetCandidateTriples(suitableCandidates);

            Dictionary<string, List<(int, int)>> cellTriples = new Dictionary<string, List<(int, int)>>();

            foreach (var triple in allTriples)
            {
                foreach (var cell in group)
                {
                    if (sudoku[cell.Item1, cell.Item2] is List<int> candidates && candidates.Intersect(triple).Count() >= 2)
                    {
                        string candidateTriple = string.Join(",", triple);
                        if (!cellTriples.ContainsKey(candidateTriple))
                        {
                            cellTriples[candidateTriple] = new List<(int, int)>();
                        }
                        cellTriples[candidateTriple].Add(cell);
                    }
                }
            }

            foreach (KeyValuePair<string, List<(int, int)>> triple in cellTriples)
            {
                if (triple.Value.Count == 3)
                {
                    string[] tripleOfCandidates = triple.Key.Split(',');
                    List<int> threeNumbers = tripleOfCandidates.Select(int.Parse).ToList();

                    //check that no other cell in the group contains any of the numbers in the triple
                    bool isValidTriple = true;
                    foreach (var cell in group)
                    {
                        if (!triple.Value.Contains(cell))
                        {
                            foreach (int number in threeNumbers)
                            {
                                if (sudoku[cell.Item1,cell.Item2] is List<int> candidates && candidates.Contains(number))
                                {
                                    isValidTriple = false;
                                    break;
                                }
                            }
                        }
                    }

                    if (isValidTriple)
                    {
                        foreach (var cell in triple.Value)
                        {
                            if (sudoku[cell.Item1, cell.Item2] is List<int> candidates)
                            {
                                List<int> toRemove = candidates.Except(threeNumbers).ToList();
                                count += toRemove.Count;
                                foreach (var candidate in toRemove)
                                {
                                    candidates.Remove(candidate);
                                }
                            }
                        }
                    }
                }
            }
        }

        return count;
    }
    
    // 6. intersection - includes pointing pairs and box-line reduction
    static int Intersection(object[,] sudoku, List<List<(int, int)>> allLines, List<List<(int, int)>> allBlocks)
    {
        int count = 0;
        foreach (var block in allBlocks)
        {
            foreach (var line in allLines)
            {
                List<(int, int)> both = block.Intersect(line).ToList();

                if (both.Count == 0) continue;

                List<(int, int)> onlyBlock = block.Except(both).ToList();
                List<(int, int)> onlyLine = line.Except(both).ToList();

                List<int> onlyBlockCandidates = GetAllUniqueCandidates(onlyBlock, sudoku);
                List<int> onlyLineCandidates = GetAllUniqueCandidates(onlyLine, sudoku);
                List<int> bothCandidates = GetAllUniqueCandidates(both, sudoku);

                for (int i = 1; i <= 9; i++)
                {
                    if (bothCandidates.Contains(i) && onlyBlockCandidates.Contains(i) && !onlyLineCandidates.Contains(i))
                    {
                        foreach (var cell in onlyBlock)
                        {
                            if (sudoku[cell.Item1,cell.Item2] is List<int> candidates && candidates.Contains(i))
                            {
                                candidates.Remove(i);
                                count++;
                            }
                        }
                    }

                    if (bothCandidates.Contains(i) && !onlyBlockCandidates.Contains(i) && onlyLineCandidates.Contains(i))
                    {
                        foreach (var cell in onlyLine)
                        {
                            if (sudoku[cell.Item1, cell.Item2] is List<int> candidates && candidates.Contains(i))
                            {
                                candidates.Remove(i);
                                count++;
                            }
                        }
                    }
                }
            }
        }

        return count;
    }

    // 7. x-wing
    static Dictionary<int, int> GetListFrequency(List<int> allCandidates)
    {
        Dictionary<int, int> candidateFreq = new Dictionary<int, int>();
        foreach (var candidate in allCandidates)
        {
            if (!candidateFreq.ContainsKey(candidate))
            {
                candidateFreq[candidate] = 0;
            }
            candidateFreq[candidate]++;
        }

        return candidateFreq;
    }

    static int XWing(object[,] sudoku, List<List<(int,int)>> allRows, List<List<(int, int)>> allColumns)
    {
        int count = 0;
        for (int rowi = 0; rowi < 8; rowi++)
        {
            for (int rowj = rowi + 1; rowj < 9; rowj++)
            {
                for (int coli = 0; coli < 8; coli++)
                {
                    for (int colj = coli + 1; colj < 9; colj++)
                    {
                        List<int> allCandiatesInX = new List<int>();

                        var intersect1 = allRows[rowi].Intersect(allColumns[coli]).ToList();
                        var intersect2 = allRows[rowi].Intersect(allColumns[colj]).ToList();
                        var intersect3 = allRows[rowj].Intersect(allColumns[coli]).ToList();
                        var intersect4 = allRows[rowj].Intersect(allColumns[colj]).ToList();

                        if (intersect1.Count != 1 || intersect2.Count != 1 || intersect3.Count != 1 || intersect4.Count != 1)
                            continue;

                        if (sudoku[intersect1[0].Item1, intersect1[0].Item2] is List<int> candidates1 && candidates1.Count() != 1)
                            allCandiatesInX.AddRange(candidates1);
                        else continue;

                        if (sudoku[intersect2[0].Item1, intersect2[0].Item2] is List<int> candidates2 && candidates2.Count() != 1)
                            allCandiatesInX.AddRange(candidates2);
                        else continue;

                        if (sudoku[intersect3[0].Item1, intersect3[0].Item2] is List<int> candidates3 && candidates3.Count() != 1)
                            allCandiatesInX.AddRange(candidates3);
                        else continue;

                        if (sudoku[intersect4[0].Item1, intersect4[0].Item2] is List<int> candidates4 && candidates4.Count() != 1)
                            allCandiatesInX.AddRange(candidates4);
                        else continue;

                        Dictionary<int, int> candidateFreq = GetListFrequency(allCandiatesInX);
                        foreach(KeyValuePair<int,int> pair in candidateFreq)
                        {
                            if (pair.Value == 4)
                            {
                                bool removeCandidates = true;
                                foreach (var cell in allRows[rowi])
                                {
                                    if (!intersect1.Contains(cell) && !intersect2.Contains(cell) &&
                                        sudoku[cell.Item1, cell.Item2] is List<int> rowCandidates && rowCandidates.Contains(pair.Key))
                                    {
                                        removeCandidates = false;
                                        break;
                                    }
                                }
                                foreach (var cell in allRows[rowj])
                                {
                                    if (!intersect3.Contains(cell) && !intersect4.Contains(cell) &&
                                        sudoku[cell.Item1, cell.Item2] is List<int> rowCandidates && rowCandidates.Contains(pair.Key))
                                    {
                                        removeCandidates = false;
                                        break;
                                    }
                                }
                                if (removeCandidates)
                                {
                                    foreach (var cell in allColumns[coli])
                                    {
                                        if (!intersect1.Contains(cell) && !intersect3.Contains(cell) &&
                                            sudoku[cell.Item1, cell.Item2] is List<int> colCandidates && colCandidates.Contains(pair.Key))
                                        {
                                            colCandidates.Remove(pair.Key);
                                            count++;
                                        }
                                    }
                                    foreach (var cell in allColumns[colj])
                                    {
                                        if (!intersect2.Contains(cell) && !intersect4.Contains(cell) &&
                                            sudoku[cell.Item1, cell.Item2] is List<int> colCandidates && colCandidates.Contains(pair.Key))
                                        {
                                            colCandidates.Remove(pair.Key);
                                            count++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return count;
    }

    // 8. y-wing
    //get all cells that can be 'seen' by the triple cells
    static List<(int, int)> GetRowColBlockUnion(List<(int, int)> tripleCells, List<List<(int, int)>> allHouses)
    {
        HashSet<(int, int)> unionCells = new HashSet<(int, int)>();
        foreach (var cell in tripleCells)
        {
            foreach (var house in allHouses)
            {
                if (house.Contains(cell))
                {
                    unionCells.UnionWith(house);
                }
            }
        }

        return unionCells.ToList();
    }

    static int YWing(object[,] sudoku, List<List<(int, int)>> allHouses)
    {
        int count = 0;

        List<(int, int)> listOfTwoCandidateCells = new List<(int, int)>();
        List<List<(int, int)>> listOfValidTripleCells = new List<List<(int, int)>>();

        foreach (var group in allHouses)
        {
            foreach (var cell in group)
            {
                if (sudoku[cell.Item1, cell.Item2] is List<int> candidates && candidates.Count() == 2)
                {
                    listOfTwoCandidateCells.Add(cell);
                }
            }
        } 

        foreach (var cell in listOfTwoCandidateCells)
        {
            List<List<(int, int)>> rowColBlock = GetRowColBlock(cell, allHouses);
            List<(int, int)> tripleCells = new List<(int, int)> { cell };
            List<List<(int, int)>> permutationOfTripleCells = new List<List<(int, int)>>();

            foreach (var lineOrBlock in rowColBlock)
            {
                foreach (var otherCell in lineOrBlock)
                {
                    if (otherCell != cell && 
                        sudoku[cell.Item1, cell.Item2] is List<int> candidates &&
                        sudoku[otherCell.Item1, otherCell.Item2] is List<int> otherCandidates &&
                        otherCandidates.Count() == 2 &&
                        candidates.Intersect(otherCandidates).Count() == 1)
                    {
                        tripleCells.Add(otherCell);
                    }
                }
            }

            if (tripleCells.Count < 3) continue;
            else if (tripleCells.Count == 3)
            {
                permutationOfTripleCells.Add(tripleCells);
            }
            else if (tripleCells.Count > 3)
            {
                for (int i = 1; i < tripleCells.Count - 2; i++)
                {
                    for (int j = i + 1; j < tripleCells.Count - 1; j++)
                    {
                        List<(int, int)> otherPermutation = new List<(int, int)> { tripleCells[0], tripleCells[i], tripleCells[j] };
                        permutationOfTripleCells.Add(otherPermutation);
                    }
                }
            }

            foreach (var permutation in permutationOfTripleCells)
            {
                foreach (var a in permutation)
                {
                    Console.Write("(" + a.Item1 + "," + a.Item2 + "), ");
                }
                Console.WriteLine();
                foreach (var a in permutation)
                {
                    if (sudoku[a.Item1, a.Item2] is List<int> b)
                    {
                        Console.Write("{");
                        foreach (var c in b)
                        {
                            Console.Write(c + ",");
                        }
                        Console.Write("}");
                    }
                }
                Console.WriteLine();
            }
            Console.WriteLine();

            foreach (var permutation in permutationOfTripleCells)
            {
                var candidatesA = sudoku[permutation[0].Item1, permutation[0].Item2] as List<int>;
                var candidatesB = sudoku[permutation[1].Item1, permutation[1].Item2] as List<int>;
                var candidatesC = sudoku[permutation[2].Item1, permutation[2].Item2] as List<int>;

                if (candidatesA.Intersect(candidatesB).Count() == 1 &&
                    candidatesA.Intersect(candidatesC).Count() == 1 &&
                    candidatesB.Intersect(candidatesC).Count() == 1)
                {
                    int candidateAB = candidatesA.Intersect(candidatesB).First();
                    int candidateAC = candidatesA.Intersect(candidatesC).First();
                    int candidateBC = candidatesB.Intersect(candidatesC).First();

                    if (candidateAC == candidateBC) continue;

                    bool isNotinValidList = true;

                    if (listOfValidTripleCells.Count() == 0)
                    {
                        listOfValidTripleCells.Add(permutation);
                    }
                    else
                    {
                        foreach (var validTripleCell in listOfValidTripleCells)
                        {
                            if (validTripleCell.All(permutation.Contains))
                            {
                                isNotinValidList = false;
                            }
                        }
                    }

                    if (isNotinValidList)
                    {
                        /*foreach (var a in rowColBlock)
                        {
                            foreach (var b in a)
                            {
                                Console.Write("(" + b.Item1 + ", " + b.Item2 + "), ");
                            }
                        }
                        Console.WriteLine();

                        foreach (var tripleCell in permutation)
                        {
                            Console.Write("(" + tripleCell.Item1 + "," + tripleCell.Item2 + "), ");
                        }
                        Console.WriteLine();
                        Console.Write(candidateAB + ", ");
                        Console.Write(candidateAC + ", ");
                        Console.Write(candidateBC + ", ");
                        Console.WriteLine();

                        List<(int, int)> allCells = GetRowColBlockUnion(permutation, allHouses);
                        foreach (var targetCell in allCells)
                        {
                            if (!permutation.Contains(targetCell))
                            {
                                if (sudoku[targetCell.Item1, targetCell.Item2] is List<int> targetCandidates)
                                {
                                    if (targetCandidates.Contains(candidateAB))
                                    {
                                        targetCandidates.Remove(candidateAB);
                                        count++;
                                    }
                                    if (targetCandidates.Contains(candidateAC))
                                    {
                                        targetCandidates.Remove(candidateAC);
                                        count++;
                                    }
                                }
                            }
                        }*/
                    }
                }
            }
        }
        

return count;
}

// 9. x-cycles

// 10. swordfish

/* MAIN SOLVER
#########################################################################  */
                    static object[,] Solve(int[,] originalPuzzle, bool showText)
    {
        int[] scoreReport = new int[10];
        
        List<List<(int, int)>> allRows = GenerateAllRows();
        List<List<(int, int)>> allColumns = GenerateAllColumns();
        List<List<(int, int)>> allBlocks = GenerateAllBlocks();
        List<List<(int, int)>> allLines = GenerateAllLines();
        List<List<(int, int)>> allHouses = GenerateAllHouses();

        object[,] puzzle = PencilInNumbers(originalPuzzle);
        int solved = CountSolved(puzzle);
        int toRemove = CountToRemove(puzzle);

        if (showText)
        {
            Console.WriteLine("Initial Puzzle \nComplete Cells: " + solved + "/81 \nCandidates to Remove: " + toRemove + "\n");
        }

        int limit = 0;
        while (toRemove != 0)
        {
            int reportStep = 0;
            int r0 = SimpleElimination(puzzle, allHouses);
            scoreReport[0] += r0;
            reportStep += r0;
            Console.WriteLine("tried elimination");

            if (reportStep == 0)
            {
                int r1 = HiddenSingles(puzzle, allHouses);
                scoreReport[1] += r1;
                reportStep += r1;
                Console.WriteLine("tried hidden singles");
            }

            if (reportStep == 0)
            {
                int r2 = NakedPairs(puzzle, allHouses);
                scoreReport[2] += r2;
                reportStep += r2;
                Console.WriteLine("tried naked pairs");
            }

            if (reportStep == 0)
            {
                int r3 = NakedTriples(puzzle, allHouses);
                scoreReport[3] += r3;
                reportStep += r3;
                Console.WriteLine("tried naked triples");
            }

            if (reportStep == 0)
            {
                int r4 = HiddenPairs(puzzle, allHouses);
                scoreReport[4] += r4;
                reportStep += r4;
                Console.WriteLine("tried hidden pairs");
            }

            if (reportStep == 0)
            {
                int r5 = HiddenTriples(puzzle, allHouses);
                scoreReport[5] += r5;
                reportStep += r5;
                Console.WriteLine("tried hidden triples");
            }

            if (reportStep == 0)
            {
                int r6 = Intersection(puzzle, allLines, allBlocks);
                scoreReport[6] += r6;
                reportStep += r6;
                Console.WriteLine("tried intersection");
            }

            if (reportStep == 0)
            {
                int r7 = XWing(puzzle, allRows, allColumns);
                scoreReport[7] += r7;
                reportStep += r7;
                Console.WriteLine("tried xwing");
            }

            if (reportStep == 0)
            {
                int r8 = YWing(puzzle, allHouses);
                scoreReport[8] += r8;
                reportStep += r8;
                Console.WriteLine("tried ywing");
            }

            // check state of puzzle
            solved = CountSolved(puzzle);
            toRemove = CountToRemove(puzzle);
            Console.WriteLine(reportStep);
            Console.WriteLine();
            PrintSudokuWithCandidates(puzzle);
            Console.WriteLine();

            //if reportStep still 0, nothing worked
            if (reportStep == 0)
            {
                break;
            }

            limit++;
            if (limit > 50)
            {
                break; 
            }
        }

        if (toRemove != 0)
        {
            Console.WriteLine("Puzzle Unsolveable with Current Methods");
        }

        string[] legend = new string[] {"Simple Elimination",
                                        "Hidden Single",
                                        "Naked Pairs", 
                                        "Naked Triples",
                                        "Hidden Pairs",
                                        "Hidden Triples",
                                        "Intersection",
                                        "X-Wing",
                                        "Y-Wing",
                                        "Swordfish"};

        // final score counter
        int[] points = new int[] {1, 5, 10, 10, 20, 20, 50, 0, 0, 0};
        int finalScore = 0;
        for (int i = 0; i < legend.Length; i++)
        {
            finalScore += scoreReport[i] * points[i];
        }

        if (showText)
        {
            Console.WriteLine("Solved with Methods \nComplete Cells: " + solved + "/81 \nCandidates to Remove: " + toRemove, "\n");
            Console.WriteLine("\nMethods Used:");
            for (int i = 0; i < legend.Length; i++)
            {
                Console.WriteLine(legend[i] + ": " + scoreReport[i]);
            }
            Console.WriteLine("Final Score: " + finalScore);
        }

        return puzzle;
    }

    static void PrintSudokuWithCandidates(object[,] sudoku)
    {
        // Loop through each row
        for (int subrow = 0; subrow < 27; subrow++)
        {
            // Print horizontal separator after every third row
            if (subrow % 3 == 0 && subrow != 0)
            {
                Console.WriteLine("---------------------------------------------------");
            }

            // Loop through each column
            for (int subcol = 0; subcol < 27; subcol++)
            {
                // Print vertical separator after every third column
                if (subcol % 3 == 0 && subcol != 0)
                {
                    Console.Write(" | ");
                }

                // Get the candidates for the current cell
                if (sudoku[subrow / 3, subcol / 3] is List<int> candidates)
                {
                    if (candidates.Contains((subrow % 3) * 3 + subcol % 3 + 1))
                    {
                        Console.Write((subrow % 3) * 3 + subcol % 3 + 1);
                    }
                    else
                    {
                        Console.Write(0);
                    }
                }
            }
            // Move to the next line after printing each row
            Console.WriteLine();
        }
    }

    static void Main(string[] args)
    {
        try
        {
            Mainline();
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine(ex.Message);
        }
        
    }

    static void Mainline()
    {
        string solvedPuzzleString = "";
        Console.WriteLine("Enter the puzzle in string form:");

        if (Console.ReadLine() is string originalPuzzleString)
        {
            int[,] originalPuzzle = ConvertToPuzzleArray(originalPuzzleString);
            object[,] solvedPuzzle = Solve(originalPuzzle, true);
            solvedPuzzleString = ConvertToString(solvedPuzzle);
        }

        Console.WriteLine();
        Console.WriteLine("Final Solution: \n");
        for (int i = 0; i < 9; i++)
        {
            Console.WriteLine(solvedPuzzleString.Substring(i * 9, 9));
        }
        Mainline();
    }
}