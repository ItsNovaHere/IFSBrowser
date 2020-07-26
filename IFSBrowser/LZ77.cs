using System;
using System.Collections.Generic;

namespace IFSBrowser {

	// ReSharper disable All
	// I didn't write this :D
	// https://github.com/BtbN/ClanServer/blob/master/eAmuseCore/Compression/LZ77.cs
	public static class LZ77 {
		private const int MinLength = 3;

		public static byte[] Decompress(byte[] data) {
			var res = new List<byte>();

			var pos = 0;
			var state = 0;

			while (pos < data.Length) {
				state >>= 1;
				if (state <= 1) {
					state = data[pos++] | 0x100;
				}

				if ((state & 1) != 0) {
					res.Add(data[pos++]);
				} else {
					var byte1 = data[pos++];
					var byte2 = data[pos++];

					var length = (byte2 & 0xf) + MinLength;
					var distance = (byte1 << 4) | (byte2 >> 4);

					if (distance == 0) {
						break;
					}

					var resPos = res.Count;
					for (var i = 0; i < length; ++i) {
						var o = resPos - distance + i;
						res.Add(o < 0 ? (byte) 0 : res[o]);
					}
				}
			}

			return res.ToArray();
		}

		private static Match FindLongestMatch(byte[] data, int offset, int windowSize, int lookAhead, int minLength) {
			var res = new Match {
				Distance = -1,
				Length = -1
			};

			var maxLength = Math.Min(lookAhead, data.Length - offset);

			for (var matchLength = maxLength; matchLength >= minLength; --matchLength)
			for (var distance = 1; distance <= windowSize; ++distance) {
				if (data.RepeatingSequencesEqual(offset, matchLength, offset - distance, distance)) {
					res.Distance = distance;
					res.Length = matchLength;
					return res;
				}
			}

			return res;
		}

		private static bool RepeatingSequencesEqual(this IReadOnlyList<byte> arr, int matchOffset, int matchLength,
			int compOffset,
			int compLength) {
			for (var i = 0; i < matchLength; ++i) {
				if (arr.GV(matchOffset + i) != arr.GV(compOffset + i % compLength)) {
					return false;
				}
			}

			return true;
		}

		private static byte GV(this IReadOnlyList<byte> arr, int i) {
			return i < 0 ? (byte) 0 : arr[i];
		}

		public static byte[] Compress(byte[] data, int windowSize = 256, int lookAhead = 0xf + MinLength) {
			if (lookAhead < MinLength || lookAhead > 0xf + MinLength) {
				throw new ArgumentException("lookAhead out of range", nameof(lookAhead));
			}

			if (windowSize < lookAhead) {
				throw new ArgumentException("windowSize needs to be larger than lookAhead", nameof(windowSize));
			}

			var result = new byte[data.Length + data.Length / 8 + 4];
			var resOffset = 1;
			var resStateOffset = 0;
			var resStateShift = 0;
			var offset = 0;

			while (offset < data.Length) {
				var match = FindLongestMatch(data, offset, windowSize, lookAhead, MinLength);
				if (match.Length >= MinLength && match.Distance > 0) {
					var binLength = match.Length - MinLength;

#if DEBUG
					if (binLength > 0xf || match.Distance > 0xfff || match.Length > data.Length - offset) {
						throw new Exception("INTERNAL ERROR: found match is invalid!");
					}
#endif

					result[resOffset++] = (byte) (match.Distance >> 4);
					result[resOffset++] = (byte) (((match.Distance & 0xf) << 4) | binLength);
					resStateShift += 1;
					offset += match.Length;
				} else {
					result[resStateOffset] |= (byte) (1 << resStateShift++);
					result[resOffset++] = data[offset++];
				}

				if (resStateShift >= 8) {
					resStateShift = 0;
					resStateOffset = resOffset++;
				}
			}

			Array.Resize(ref result, resOffset + 2);

			return result;
		}

		private struct Match {
			public int Distance;
			public int Length;
		}
	}

}