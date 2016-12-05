using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using SparrowXNA;

namespace AwesomePirates
{
    class HiScoreTable
    {
        public const int kLeaderboardFontSize = 20;

        private const string kDataVersion = "Version_1.0";
        private const string kSaveFileName = "HiScores.dat";

        public HiScoreTable(string fontName, int maxPosition = 100, int offsetPosition = 0)
        {
            mFontName = fontName;
            mMaxPosition = Math.Max(1, maxPosition);
            mOffsetPosition = Math.Max(0, offsetPosition);
            mScores = new List<Score>(mMaxPosition);
        }

        #region Fields
        private int mOffsetPosition;
        private int mMaxPosition;
        private string mFontName;
        private List<Score> mScores;
        private Dictionary<string, string> mLocalScorers;
        #endregion

        #region Properties
        public int NumScores { get { return (mScores != null) ? mScores.Count : 0; } }
        public Score BestScore
        {
            get
            {
                if (mScores != null && mScores.Count > 0)
                    return new Score(mScores[0].value, mScores[0].name);
                else
                    return new Score();
            }
        }
        #endregion

        #region Methods
        public void SetLocalScorers(Dictionary<string, string> scorers)
        {
            mLocalScorers = scorers;
        }

        private void CropTable(int maxPos)
        {
            int numScores = NumScores;
            if (numScores > maxPos)
                mScores.RemoveRange(maxPos, numScores - maxPos); 
        }

        private int FindPositionForScore(Score score, List<Score> scores)
        {
#if false
            int index = 0;
            foreach (Score closestScore in mScores)
            {
                if (score.value > closestScore.value)
                    break;

                ++index;
            }

            return index;
#else
            int lower = 0, upper = scores.Count, probe = 0;

            if (upper == 0)
                return 0;

            Score closestScore = scores[probe];
            while ((probe = (upper - lower) >> 1) != 0)
            {
                probe += lower;
                closestScore = scores[probe];

                if (closestScore.value > score.value)
                {
                    lower = probe;
                }
                else if (closestScore.value < score.value)
                {
                    upper = probe;
                }
                else
                {
                    return probe;
                }
            }

            return probe + lower;
#endif
        }

        public int RankForScore(int score)
        {
            if (mScores == null)
                return -1;

            int pos = FindPositionForScore(new Score(score), mScores);

            if (score < mScores[pos].value)
                pos += 1;

            return pos + 1;
        }

        public void PreFill(int min, int max)
        {
            Clear();

            int interval = (max - min) / mMaxPosition;
            for (int i = 0; i < mMaxPosition; ++i)
                mScores.Add(new Score(max - i * interval, Score.kDefaultScoreName));
        }

        public void PopulateRanks(Func<int, int> ranker)
        {
            if (mScores != null && ranker != null)
            {
                for (int i = 0; i < mScores.Count; ++i)
                {
                    Score score = mScores[i];
                    score.rank = ranker(i);
                    mScores[i] = score;
                }
            }
        }

        public int InsertScore(int value, string name)
        {
            return InsertScore(new Score(value, name));
        }

        public int InsertScore(Score score)
        {
            if (mScores == null)
                return -1;

            int pos = FindPositionForScore(score, mScores);

            // Discard scores that don't qualify
            if (pos >= mMaxPosition)
                pos = -1;

            if (pos != -1)
            {
                if (pos >= mScores.Count)
                {
                    pos = mScores.Count;
                    mScores.Insert(pos, score);
                }
                else
                {
                    Score closestScore = mScores[pos];

                    if (closestScore.value > score.value)
                    {
                        pos += 1;

                        if (pos >= mMaxPosition)
                            pos = -1;
                        else
                            mScores.Insert(pos, score);
                    }
                    else if (closestScore.value < score.value)
                    {
                        mScores.Insert(pos, score);
                    }
                    else if (closestScore.name != null && !closestScore.name.Equals(score.name))
                    {
                        // Earliest score takes precedence
                        pos += 1;

                        if (pos >= mMaxPosition)
                            pos = -1;
                        else
                            mScores.Insert(pos, score);
                    }
                }

                CropTable(mMaxPosition);
            }

            return pos;
        }

        public List<Score> BestScores(int count)
        {
            if (mScores == null || count <= 0)
                return new List<Score>();
            else
                return new List<Score>(mScores.GetRange(0, Math.Min(mScores.Count, count)));
        }

        public bool IsScoreUniqueAndRankable(Score score)
        {
            if (mScores != null)
            {
                foreach (Score s in mScores)
                {
                    if (s.name == score.name && s.value == score.value)
                        return false;
                }

                int pos = FindPositionForScore(score, mScores);
                return pos != -1 && pos < mMaxPosition;
            }
            else
                return false;
        }

        public Score HighestScoreForPlayer(string gamertag)
        {
            foreach (Score score in mScores)
            {
                if (score.name == gamertag)
                    return score;
            }

            return new Score(0, gamertag);
        }

        public int HighestRankForPlayer(string gamertag)
        {
            foreach (Score score in mScores)
            {
                if (score.name == gamertag)
                    return score.rank;
            }

            return -1;
        }

        public void Clear()
        {
            if (mScores == null)
                return;
            mScores.Clear();
        }

        public SPDisplayObject HiScoreCellForIndex(int index, SceneController scene)
        {
            SPSprite sprite = new SPSprite();

            if (mScores == null || index < 0 || index >= mScores.Count)
                return sprite;

            Score score = mScores[index];

            SPImage bgImage = new SPImage(scene.TextureByName(((index & 1) == 0) ? "tableview-cell-light" : "tableview-cell-dark"));
            bgImage.ScaleX = 560f / bgImage.Width;
            bgImage.ScaleY = 64f / bgImage.Height;
            sprite.AddChild(bgImage);
#if false
            if (index < 10)
                bgImage.Color = ((index & 1) == 0) ? SPUtils.ColorFromColor(0x487ee0) : SPUtils.ColorFromColor(0x6495ed);
            else if (index < 20)
                bgImage.Color = ((index & 1) == 0) ? SPUtils.ColorFromColor(0xe03737) : SPUtils.ColorFromColor(0xed6b64);
            else if (index < 30)
                bgImage.Color = ((index & 1) == 0) ? SPUtils.ColorFromColor(0x1e7c1c) : SPUtils.ColorFromColor(0x4cb049);
#endif
            float descOffsetX = 0;
            int rank = score.rank != -1 ? score.rank : index + mOffsetPosition;
            if (rank < 3)
            {
                int trophyIndex = rank; // index < 3 ? index : 3;
                SPImage hiScoreImage = new SPImage(scene.TextureByName("sk-trophy-" + trophyIndex.ToString()));
                hiScoreImage.X = 6;
                hiScoreImage.Y = (bgImage.Height - hiScoreImage.Height) / 2;
                sprite.AddChild(hiScoreImage);
                descOffsetX = hiScoreImage.Width;
            }

            string descNumeral = (rank < 3) ? "" : (rank + 1).ToString() + ". ";
            SPTextField descText = new SPTextField(330 - 0.85f * descOffsetX, 40, "", mFontName, kLeaderboardFontSize);
            descText.Text = Locale.SanitizeTextForDisplay(Locale.SanitizeText(descNumeral + score.name, SceneController.LeaderboardFontKey, kLeaderboardFontSize),
                    descText.Font, descText.Width / descText.FontPtScale.X - 1);
            descText.X = descOffsetX + 14;
            descText.Y = -1 + (bgImage.Height - descText.Height) / 2;
            descText.HAlign = SPTextField.SPHAlign.Left;
            descText.VAlign = SPTextField.SPVAlign.Center;
            descText.Color = mLocalScorers == null || !mLocalScorers.ContainsKey(score.name) ? Color.Black : Color.Green;
            sprite.AddChild(descText);

            SPTextField valueText = new SPTextField(220, 40, GuiHelper.CommaSeparatedValue(score.value), mFontName, 26);
            valueText.X = bgImage.Width - (valueText.Width + 14);
            valueText.Y = -2 + (bgImage.Height - valueText.Height) / 2;
            valueText.HAlign = SPTextField.SPHAlign.Right;
            valueText.VAlign = SPTextField.SPVAlign.Center;
            valueText.Color = Color.Black;
            sprite.AddChild(valueText);

            if (index < mScores.Count - 1)
            {
                SPImage separatorImage = new SPImage(scene.TextureByName("tableview-cell-divider"));
                separatorImage.ScaleX = bgImage.ScaleX;
                separatorImage.Y = bgImage.Y + bgImage.Height - separatorImage.Height;
                sprite.AddChild(separatorImage);
            }

            return sprite;
        }

        public void Load()
        {
            try
            {
                if (FileManager.FM.IsReadyGlobal() && FileManager.FM.FileExistsGlobal(FileManager.kSharedStorageContainerName, kSaveFileName))
                {
                    Clear();
                    FileManager.FM.LoadGlobal(FileManager.kSharedStorageContainerName, kSaveFileName, stream =>
                    {
                        using (BinaryReader reader = new BinaryReader(stream))
                            DecodeWithReader(reader);
                        Debug.WriteLine("Hi scores load completed.");
                    });
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("An unexpected error occurred when attempting to load hi scores. " + e.Message);
            }
            finally
            {
                CropTable(mMaxPosition);
            }
        }

        public void Save()
        {
            if (mScores == null)
                return;

            try
            {
                HiScoreTable clone = Clone();
                FileManager.FM.QueueGlobalSaveAsync(FileManager.kSharedStorageContainerName, kSaveFileName, stream =>
                {
                    try
                    {
                        using (BinaryWriter writer = new BinaryWriter(stream))
                            clone.EncodeWithWriter(writer);
                        Debug.WriteLine("Hi scores save completed.");
                    }
                    catch (Exception eInner)
                    {
                        Debug.WriteLine(eInner.Message);
                    }
                });
            }
            catch (Exception e)
            {
                Debug.WriteLine("An unexpected error occurred when attempting to save hi scores. " + e.Message);
            }
        }

        public virtual HiScoreTable Clone()
        {
            HiScoreTable clone = MemberwiseClone() as HiScoreTable;
            clone.mScores = new List<Score>(mScores);
            return clone;
        }

        private void DecodeWithReader(BinaryReader reader)
        {
            int i, count;

            // Decrypt buffer
            count = reader.ReadInt32();

            if (count > 50000)
                throw new Exception("Hi score data length is invalid. Loading aborted.");

            byte[] buffer = new byte[count];
            int bufferLen = reader.Read(buffer, 0, count);

            if (bufferLen != count)
                throw new Exception("Hi scores could not be loaded due to file length inaccuracies.");
            FileManager.MaskUnmaskBuffer(0x25, buffer, bufferLen);

            BinaryReader br = new BinaryReader(new MemoryStream(buffer));

            // Read Saved Data
            string dataVersion = br.ReadString();
            count = br.ReadInt32();

            if (mScores == null)
                mScores = new List<Score>();

            for (i = 0; i < count; ++i)
            {
                int value = br.ReadInt32();
                string name = br.ReadString();
                mScores.Add(new Score(value, name));
            }

            buffer = null;
            br = null;
        }

        private void EncodeWithWriter(BinaryWriter writer)
        {
            if (mScores == null || mScores.Count == 0)
                return;

            BinaryWriter bw = new BinaryWriter(new MemoryStream(2500));
            bw.Write(kDataVersion);
            bw.Write(mScores.Count);

            foreach (Score score in mScores)
            {
                bw.Write(score.value);
                bw.Write(score.name);
            }

            // Perform basic encryption on buffer
            Stream stream = bw.BaseStream;
            stream.Position = 0;

            byte[] buffer = new byte[(int)stream.Length];
            int bufferLen = stream.Read(buffer, 0, (int)stream.Length);
            FileManager.MaskUnmaskBuffer(0x25, buffer, bufferLen);

            // Write encrypted buffer back to stream
            writer.Write(bufferLen);
            writer.Write(buffer, 0, bufferLen);

            buffer = null;
            bw = null;
        }
        #endregion
    }
}
