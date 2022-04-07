using System;
using System.Collections.Generic;
using System.Text;
using PokemonPRNG.XorShift128;

namespace PokemonBDSPRNGLibrary.RestoreSeed
{
    public class PlayerLinearSearch
    {
        private readonly List<int> intervals = new List<int>();

        public void AddInterval(int interval)
        {
            intervals.Add(interval);
        }
        public IEnumerable<(uint index, (uint s0, uint s1, uint s2, uint s3) state)> Search((uint s0, uint s1, uint s2, uint s3) state, uint max)
        {
            var blinkCache = new int[256];
            var lastBlink = -1;
            var idx = 0;
            for(int i = 0; i < intervals.Count; idx++)
            {
                if (state.BlinkPlayer() != PlayerBlink.None)
                {
                    if (lastBlink != -1)
                    {
                        var b = idx - lastBlink;
                        blinkCache[i] = b;
                        i++;
                    }
                    lastBlink = idx;
                }
            }
            bool check(int k)
            {
                for (int i = 0; i < intervals.Count; i++)
                {
                    var b = blinkCache[(k + i) & 0xFF];
                    if (intervals[i] != b) return false;
                }

                return true;
            }
            int head = 0, tail = intervals.Count;
            while (head <= max)
            {
                while (true)
                {
                    idx++;
                    if (state.BlinkPlayer() != PlayerBlink.None)break;
                }
                var interval = idx - lastBlink;
                blinkCache[tail++ & 0xFF] = interval;
                if (check(head++)) yield return ((uint)lastBlink, state);
                lastBlink = idx;
            }
        }

        public IEnumerable<(uint index, double nextPokeBlink, (uint s0, uint s1, uint s2, uint s3) state)> SearchInNoisy((uint s0, uint s1, uint s2, uint s3) state, uint max, double dt)
        {
            for (uint i = 0; i < max; i++)
            {
                var b = state.BlinkPlayer();
                if (b == PlayerBlink.None) continue;
                var rand = state;
                double starting = 0.0;
                while (starting < 12.3)
                {
                    //pktimer:�|�P�����u���̔����^�C�~���O
                    var pktimer = starting;
                    //offset:�O���l�����u�����Ă���|�P�������u��������
                    var offset = 0;
                    //t:�o�ߎ���
                    var t = 0.0;
                    //prevIdx:�O��̎�l���̏u��
                    var prevIdx = i;
                    //j:���݂�state����_�Ƃ���advance(����)
                    var j = prevIdx;
                    //c:��l���̏u���J�E���^
                    var c = 0;
                    while (c<intervals.Count)
                    {
                        t += 61.0 / 60.0;
                        j++;
                        if (pktimer < t)
                        {
                            var pkInterval = rand.BlinkPokemon();
                            pktimer += pkInterval;
                            j++;
                            offset++;
                        }
                        if (rand.BlinkPlayer() != PlayerBlink.None)
                        {
                            var interval = j - prevIdx - offset;
                            if (interval != intervals[c])
                                break;
                            offset = 0;
                            prevIdx = j;
                        }
                    }
                    if (c == intervals.Count)
                    {
                        var index = j;
                        var nextPokeBlink = pktimer - t;
                        yield return (index, nextPokeBlink, rand);
                    }
                    starting += dt;
                }
            }
        }
    }
}