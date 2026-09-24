#if !NET5_0_OR_GREATER
#pragma warning disable IDE0130
namespace System.Runtime.CompilerServices {
#pragma warning restore IDE0130
    internal static class IsExternalInit {
    }
}
#endif

#if !NET6_0_OR_GREATER
#pragma warning disable IDE0130
namespace System.Runtime.CompilerServices {
#pragma warning restore IDE0130
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    internal sealed class CallerArgumentExpressionAttribute(string parameterName) : Attribute {
        public string ParameterName { get; } = parameterName;
    }
}
#endif

#if !NET7_0_OR_GREATER
#pragma warning disable IDE0130
namespace System.Runtime.CompilerServices {
#pragma warning restore IDE0130
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    internal sealed class RequiredMemberAttribute : Attribute {
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    internal sealed class CompilerFeatureRequiredAttribute(string featureName) : Attribute {
        public const string RefStructs = nameof(RefStructs);

        public const string RequiredMembers = nameof(RequiredMembers);

        public string FeatureName { get; } = featureName;

        public bool IsOptional { get; init; }
    }
}

#pragma warning disable IDE0130
namespace System.Diagnostics.CodeAnalysis {
#pragma warning restore IDE0130
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    internal sealed class SetsRequiredMembersAttribute : Attribute {
    }
}
#endif

#if !NETCOREAPP3_0_OR_GREATER && !NETSTANDARD2_1_OR_GREATER
#pragma warning disable IDE0130
namespace System.Diagnostics.CodeAnalysis {
#pragma warning restore IDE0130
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
    internal sealed class NotNullAttribute : Attribute {
    }
}

#pragma warning disable IDE0130
namespace System.Text {
#pragma warning restore IDE0130
    using System.Buffers;

    internal static class EncodingPolyfills {
        internal static string GetString(this Encoding encoding, ReadOnlySpan<byte> bytes) {
            if (bytes.IsEmpty) {
                return string.Empty;
            }

            byte[] buffer = ArrayPool<byte>.Shared.Rent(bytes.Length);

            try {
                bytes.CopyTo(buffer);
                return encoding.GetString(buffer, 0, bytes.Length);
            }
            finally {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}

#pragma warning disable IDE0130
namespace System.Buffers {
#pragma warning restore IDE0130
    internal sealed class ArrayBufferWriter<T> : IBufferWriter<T> {
        private const int DEFAULT_INITIAL_CAPACITY = 256;

        private T[] _buffer = [];

        private int _index;

        public ReadOnlySpan<T> WrittenSpan => _buffer.AsSpan(0, _index);

        public ReadOnlyMemory<T> WrittenMemory => _buffer.AsMemory(0, _index);

        public int WrittenCount => _index;

        public void Advance(int count) {
            if (count < 0) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (_index > _buffer.Length - count) {
                throw new InvalidOperationException("Cannot advance past the end of the buffer.");
            }

            _index += count;
        }

        public Memory<T> GetMemory(int sizeHint = 0) {
            EnsureCapacity(sizeHint);
            return _buffer.AsMemory(_index);
        }

        public Span<T> GetSpan(int sizeHint = 0) {
            EnsureCapacity(sizeHint);
            return _buffer.AsSpan(_index);
        }

        private void EnsureCapacity(int sizeHint) {
            if (sizeHint < 0) {
                throw new ArgumentOutOfRangeException(nameof(sizeHint));
            }

            if (sizeHint == 0) {
                sizeHint = 1;
            }

            if (sizeHint <= _buffer.Length - _index) {
                return;
            }

            int growBy = Math.Max(sizeHint, Math.Max(_buffer.Length, DEFAULT_INITIAL_CAPACITY));
            Array.Resize(ref _buffer, checked(_buffer.Length + growBy));
        }
    }

    internal ref struct SequenceReader<T> where T : unmanaged, IEquatable<T> {
        private SequencePosition _currentPosition;

        private SequencePosition _nextPosition;

        private bool _moreData;

        private readonly long _length;

        public SequenceReader(ReadOnlySequence<T> sequence) {
            Sequence = sequence;
            CurrentSpan = default;
            CurrentSpanIndex = 0;
            Consumed = 0;
            _length = sequence.Length;
            _currentPosition = sequence.Start;
            _nextPosition = sequence.Start;
            _moreData = true;

            if (sequence.TryGet(ref _nextPosition, out ReadOnlyMemory<T> memory, advance: true) && memory.Length > 0) {
                CurrentSpan = memory.Span;
            }
            else {
                GetNextSpan();
            }
        }

        public readonly ReadOnlySequence<T> Sequence { get; }

        public readonly SequencePosition Position => Sequence.GetPosition(CurrentSpanIndex, _currentPosition);

        public ReadOnlySpan<T> CurrentSpan { readonly get; private set; }

        public int CurrentSpanIndex { readonly get; private set; }

        public readonly ReadOnlySpan<T> UnreadSpan => CurrentSpan.Slice(CurrentSpanIndex);

        public long Consumed { readonly get; private set; }

        public readonly long Remaining => _length - Consumed;

        public readonly bool End => !_moreData;

        public bool TryRead(out T value) {
            if (End) {
                value = default;
                return false;
            }

            value = CurrentSpan[CurrentSpanIndex];
            CurrentSpanIndex++;
            Consumed++;

            if (CurrentSpanIndex >= CurrentSpan.Length) {
                GetNextSpan();
            }

            return true;
        }

        public void Advance(long count) {
            if (count < 0) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (count < CurrentSpan.Length - CurrentSpanIndex) {
                CurrentSpanIndex += (int)count;
                Consumed += count;
                return;
            }

            if (count > Remaining) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            Consumed += count;

            while (_moreData) {
                int available = CurrentSpan.Length - CurrentSpanIndex;

                if (available > count) {
                    CurrentSpanIndex += (int)count;
                    return;
                }

                CurrentSpanIndex += available;
                count -= available;
                GetNextSpan();

                if (count == 0) {
                    return;
                }
            }
        }

        public readonly bool TryCopyTo(Span<T> destination) {
            ReadOnlySpan<T> first = UnreadSpan;

            if (first.Length >= destination.Length) {
                first.Slice(0, destination.Length).CopyTo(destination);
                return true;
            }

            if (Remaining < destination.Length) {
                return false;
            }

            first.CopyTo(destination);
            int copied = first.Length;
            SequencePosition next = _nextPosition;

            while (copied < destination.Length && Sequence.TryGet(ref next, out ReadOnlyMemory<T> segment, advance: true)) {
                ReadOnlySpan<T> span = segment.Span;
                int count = Math.Min(span.Length, destination.Length - copied);
                span.Slice(0, count).CopyTo(destination.Slice(copied));
                copied += count;
            }

            return true;
        }

        private void GetNextSpan() {
            if (!Sequence.IsSingleSegment) {
                SequencePosition previous = _nextPosition;

                while (Sequence.TryGet(ref _nextPosition, out ReadOnlyMemory<T> memory, advance: true)) {
                    _currentPosition = previous;

                    if (memory.Length > 0) {
                        CurrentSpan = memory.Span;
                        CurrentSpanIndex = 0;
                        return;
                    }

                    CurrentSpan = default;
                    CurrentSpanIndex = 0;
                    previous = _nextPosition;
                }
            }

            _moreData = false;
        }
    }
}
#endif

#if !NET8_0_OR_GREATER
#pragma warning disable IDE0130
namespace System.Text.Unicode {
#pragma warning restore IDE0130
    internal static class Utf8 {
        internal static bool IsValid(ReadOnlySpan<byte> value) {
            int i = 0;

            while (i < value.Length) {
                byte lead = value[i];

                if (lead < 0x80) {
                    i++;
                    continue;
                }

                int trailing;

                if ((lead & 0xE0) == 0xC0) {
                    if (lead < 0xC2) {
                        return false;
                    }

                    trailing = 1;
                }
                else if ((lead & 0xF0) == 0xE0) {
                    trailing = 2;
                }
                else if ((lead & 0xF8) == 0xF0) {
                    if (lead > 0xF4) {
                        return false;
                    }

                    trailing = 3;
                }
                else {
                    return false;
                }

                if (i + trailing >= value.Length) {
                    return false;
                }

                byte second = value[i + 1];

                if ((second & 0xC0) != 0x80 ||
                    (lead == 0xE0 && second < 0xA0) ||
                    (lead == 0xED && second > 0x9F) ||
                    (lead == 0xF0 && second < 0x90) ||
                    (lead == 0xF4 && second > 0x8F)) {
                    return false;
                }

                for (int k = 2; k <= trailing; k++) {
                    if ((value[i + k] & 0xC0) != 0x80) {
                        return false;
                    }
                }

                i += trailing + 1;
            }

            return true;
        }
    }
}
#endif
