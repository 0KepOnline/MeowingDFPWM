namespace MeowingDFPWM
{
    /// <summary>
    /// DFPWM audio steam.
    /// </summary>
    public class DFPWMStream : MemoryStream
    {
        /// <summary>
        /// Gets or sets the encoding profile defining DFPWM parameters.
        /// </summary>
        /// 
        /// <returns>
        /// The profile used for encoding and decoding the DFPWM stream.
        /// </returns>
        /// 
        public Profile Profile { get; }

        /// <summary>
        /// Gets or sets the sample rate of the stream.
        /// </summary>
        /// 
        /// <returns>
        /// The sample rate of the DFPWM stream, which is used to determine
        /// the playback speed and quality.
        /// </returns>
        /// 
        /// <exception cref="ArgumentException">
        /// Sample rate must be greater than zero.
        /// </exception>
        public uint SampleRate
        {
            get => _sampleRate;
            set
            {
                if (value == 0)
                    throw new ArgumentException("Sample rate must be greater than zero.",
                        nameof(value));
                _sampleRate = value;
            }
        }

        uint _sampleRate;

        private bool _bitValuePrevious = false;
        private byte _response = 0;
        private sbyte _level, _levelPrevious, _levelLowPassFiltered = 0;

        private bool _suppressAutoAdapt = false;


        /// <summary>
        /// Creates a DFPWM stream with the default profile and sample rate (DFPWM1a/48kHz).
        /// </summary>
        public DFPWMStream() : this(Profile.DFPWM1a, Profile.DFPWM1a.BaseSampleRate) { }

        /// <summary>
        /// Creates a DFPWM stream with a specified profile.
        /// </summary>
        /// 
        /// <param name="profile">
        /// The specified DFPWM profile.
        /// </param>
        /// 
        /// <exception cref="ArgumentNullException">
        /// </exception>
        public DFPWMStream(Profile profile) : this(profile, profile.BaseSampleRate) { }

        /// <summary>
        /// Creates a DFPWM stream with a specified profile and sample rate.
        /// </summary>
        /// 
        /// <param name="profile">
        /// The specified DFPWM profile.
        /// </param>
        /// <param name="sampleRate">
        /// The specified sample rate. Must be greater than zero.
        /// </param>
        /// 
        /// <exception cref="ArgumentNullException">
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Sample rate must be greater than zero.
        /// </exception>
        public DFPWMStream(Profile profile, uint sampleRate)
        {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            if (sampleRate == 0)
                throw new ArgumentException("Sample rate must be greater than zero.", nameof(sampleRate));
            SampleRate = sampleRate;
        }


        public override long Position
        {
            get => base.Position;
            set
            {
                if (base.Position != value)
                {
                    if (!_suppressAutoAdapt)
                    {
                        _response = 0;
                        _bitValuePrevious = false;
                        _level = 0;

                        if (value > 0 && value < Length)
                        {
                            base.Position = 0;

                            for (long bytePosition = 0; bytePosition < value; bytePosition++)
                            {
                                int readByte = base.ReadByte();
                                if (readByte == -1)
                                    break;

                                byte packedBits = (byte)readByte;
                                for (byte bit = 0; bit < 8; bit++)
                                {
                                    bool bitValue = (packedBits & 1) != 0;
                                    AdaptToBit(bitValue);
                                    packedBits >>= 1;
                                }
                            }
                        }
                    }
                    base.Position = value;
                }
            }
        }

        /// <summary>
        /// Adapts the internal state based on the current bit value.
        /// </summary>
        /// 
        /// <param name="bitValue">
        /// The current bit value to adapt to.
        /// </param>
        private void AdaptToBit(bool bitValue)
        {
            // Calculate the target level based on the bit value
            int levelTarget = bitValue
                ? sbyte.MaxValue
                : sbyte.MinValue;
            sbyte levelAdapted = (sbyte)(_level +
                (_response * (levelTarget - _level) + (1 << Profile.ResponsePrecisionBits - 1)
                >> Profile.ResponsePrecisionBits));

            if (levelAdapted == _level && _level != levelTarget)
                levelAdapted += (sbyte)(bitValue ? 1 : -1);

            // Adjust the response based on whether the bit value changed
            bool bitChanged = bitValue != _bitValuePrevious;
            int responseTarget = bitChanged
                ? 0
                : (1 << Profile.ResponsePrecisionBits) - 1;
            byte responseAdapted = _response;
            if (Profile.ResponseIncrement != 1 && Profile.ResponseDecrement != 1)
            {
                int responseDelta = bitChanged
                    ? Profile.ResponseDecrement
                    : Profile.ResponseIncrement;
                responseAdapted = (byte)Math.Max(0, Math.Min(byte.MaxValue,
                    _response + (responseDelta * (responseTarget - _response) + 0x80 >> 8)));
            }

            if (responseAdapted == _response && _response != responseTarget)
                responseAdapted = (byte)(responseAdapted - (bitChanged ? 1 : -1));

            if (Profile.ResponsePrecisionBits > 8)
            {
                byte responseMin = (byte)(2 << Profile.ResponsePrecisionBits - 8);
                if (responseAdapted < responseMin)
                    responseAdapted = responseMin;
            }

            // Update internal state
            _response = responseAdapted;
            _bitValuePrevious = bitValue;
            _level = levelAdapted;
        }

        /// <summary>
        /// Temporarily suppresses automatic adaptation on seeking through the stream
        /// while executing an action.
        /// </summary>
        /// 
        /// <param name="action">
        /// The action to execute.
        /// </param>
        public void SuppressAutomaticAdapt(Action action)
        {
            _suppressAutoAdapt = true;
            try
            {
                action();
            }
            finally
            {
                _suppressAutoAdapt = false;
            }
        }


        /// <summary>
        /// Encodes 8-bit little endian PCM audio data into DFPWM.
        /// </summary>
        /// 
        /// <param name="pcm8leStream">
        /// The input 8-bit little endian PCM stream.
        /// </param>
        /// 
        /// <exception cref="ObjectDisposedException">
        /// </exception>
        public void Encode(Stream pcm8leStream) =>
            Encode(pcm8leStream, pcm8leStream.Length);

        /// <summary>
        /// Encodes a specified length of 8-bit little endian PCM audio data into DFPWM.
        /// </summary>
        /// 
        /// <param name="pcm8leStream">
        /// The input 8-bit little endian PCM stream.
        /// </param>
        /// <param name="length">
        /// The number of bytes to encode.
        /// </param>
        /// 
        /// <exception cref="ObjectDisposedException">
        /// </exception>
        public void Encode(Stream pcm8leStream, long length)
        {
            long bytesRead = 0;
            SuppressAutomaticAdapt(() =>
            {
                while (bytesRead < length)
                {
                    byte writeByte = 0;
                    for (byte bit = 0; bit < 8; bit++)
                    {
                        int readByte = pcm8leStream.ReadByte();
                        if (readByte == -1)
                        {
                            writeByte >>= 8 - bit;
                            break;
                        }

                        sbyte inputLevel = (sbyte)(readByte + sbyte.MinValue);
                        bool bitValue = inputLevel > _level
                            || inputLevel == _level && _level == sbyte.MaxValue;
                        writeByte = (byte)(bitValue
                            ? (writeByte >> 1) + 0x80
                            : writeByte >> 1);
                        AdaptToBit(bitValue);
                        bytesRead++;
                    }

                    WriteByte(writeByte);
                }
            });
        }


        /// <summary>
        /// Decodes DFPWM audio data into 8-bit little endian PCM.
        /// </summary>
        /// 
        /// <param name="pcm8leStream">
        /// The output 8-bit little endian PCM stream.
        /// </param>
        public void Decode(Stream pcm8leStream) =>
            Decode(pcm8leStream, Length);

        /// <summary>
        /// Decodes a specified length of DFPWM audio data into 8-bit little endian PCM.
        /// </summary>
        /// 
        /// <param name="pcm8leStream">
        /// The output 8-bit little endian PCM stream.
        /// </param>
        /// <param name="length">
        /// The number of bytes to decode.
        /// </param>
        /// 
        /// <exception cref="IOException">
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// </exception>
        public void Decode(Stream pcm8leStream, long length)
        {
            SuppressAutomaticAdapt(() =>
            {
                for (long bytesRead = 0; bytesRead < length; bytesRead++)
                {
                    int readByte = ReadByte();
                    if (readByte == -1)
                        break;

                    byte packedBits = (byte)readByte;
                    for (byte bit = 0; bit < 8; bit++)
                    {
                        bool bitValue = (packedBits & 1) != 0;
                        packedBits >>= 1;

                        bool previousBit = _bitValuePrevious;
                        AdaptToBit(bitValue);

                        sbyte blendedLevel = (sbyte)(bitValue != previousBit
                            ? _levelPrevious + _level + 1 >> 1
                            : _level);
                        _levelPrevious = _level;

                        _levelLowPassFiltered = (sbyte)Math.Max(sbyte.MinValue,
                            Math.Min(sbyte.MaxValue,
                            _levelLowPassFiltered + (Profile.LowPassFilterStrength *
                            (blendedLevel - _levelLowPassFiltered) + 0x80 >> 8)));

                        pcm8leStream.WriteByte((byte)_levelLowPassFiltered);
                    }
                }
            });
        }
    }
}
