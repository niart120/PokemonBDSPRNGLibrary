using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPRNG.XorShift128;
using static System.Console;

namespace PokemonBDSPRNGLibrary.RestoreSeed
{
    public class PlayerLinearSearch
    {
        private readonly List<uint> intervals = new List<uint>();
        public int BlinkCount { get => intervals.Count; }

        public void AddInterval(uint interval)
        {
            intervals.Add(interval);
        }
        public IEnumerable<(uint index, (uint s0, uint s1, uint s2, uint s3) state)> Search((uint s0, uint s1, uint s2, uint s3) state, uint max)
        {
            var indexQueue = new Queue<uint>();
            var intervalQueue = new Queue<uint>();

            var idx = state.GetNextPlayerBlink();
            for(int i = 0; i < intervals.Count; i++)
            {
                var interval = state.GetNextPlayerBlink();
                idx += interval;

                indexQueue.Enqueue(idx);
                intervalQueue.Enqueue(interval);
            }
            
            for (int head = 0, tail = intervals.Count; head <= max; head++, tail++)
            {
                if (intervals.SequenceEqual(intervalQueue)) yield return (idx, state);

                var interval = state.GetNextPlayerBlink();
                idx += interval;
                indexQueue.Enqueue(idx);
                intervalQueue.Enqueue(interval);
                indexQueue.Dequeue();
                intervalQueue.Dequeue();
            }
        }

        public IEnumerable<(uint index, double nextPokeBlink, (uint s0, uint s1, uint s2, uint s3) state)> SearchInNoisy((uint s0, uint s1, uint s2, uint s3) state, uint max, double dt = 1.0/60.0)
        {
            for (int i = 0; i < max; i++)
            {
                var b = state.BlinkPlayer();
                if (b == PlayerBlink.None) continue;

                var starting = 0.0;
                while (starting < 12.3)
                {
                    var rand = state;
                    // pktimer:�|�P�����u���̔����^�C�~���O
                    var pktimer = starting;
                    // offset:�O���l�����u�����Ă���|�P�������u��������
                    var offset = 0;
                    // t:�o�ߎ���
                    var t = 0.0;
                    // j:state����_�Ƃ���advance(����)
                    var j = i+1;
                    // prevIdx:�O��̎�l���̏u��
                    var prevIdx = j;
                    // c:��l���̏u���J�E���^
                    var c = 0;
                    while (c < intervals.Count)
                    {
                        t += 61.0 / 60.0;
                        if (pktimer < t)
                        {
                            var pkInterval = rand.BlinkPokemon();
                            j++;
                            pktimer += pkInterval;
                            offset++;
                        }
                        var blink = rand.BlinkPlayer();
                        j++;
                        if (blink != PlayerBlink.None)
                        {
                            var interval = j - prevIdx - offset;
                            if (interval != intervals[c])
                                break;
                            offset = 0;
                            prevIdx = j;
                            c++;
                        }
                    }
                    if (c == intervals.Count)
                    {
                        var index = j;
                        var nextPokeBlink = pktimer - t;
                        yield return ((uint)index, nextPokeBlink, rand);
                    }
                    starting += dt;
                }
            }
        }
    }
}