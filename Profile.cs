namespace MeowingDFPWM
{
    /// <summary>
    /// Represents a configuration profile for DFPWM encoding and decoding.
    /// Defines parameters for response adaptation, precision, LPF strength and sample rates.
    /// </summary>
    public sealed class Profile
    {
        /// <summary>
        /// Gets the response increment value.
        /// </summary>
        /// 
        /// <returns>
        /// The increment value for adapting the response when the bit value remains unchanged.
        /// </returns>
        public short ResponseIncrement { get; }

        /// <summary>
        /// Gets the response decrement value.
        /// </summary>
        /// 
        /// <returns>
        /// The decrement value for adapting the response when the bit value remains unchanged.
        /// </returns>
        public short ResponseDecrement { get; }

        /// <summary>
        /// Gets the number of bits used for response precision.
        /// </summary>
        /// 
        /// <returns>
        /// The number of bits used for response precision.
        /// </returns>
        public byte ResponsePrecisionBits { get; }

        /// <summary>
        /// Gets the strength of the low-pass filter.
        /// </summary>
        /// 
        /// <returns>
        /// The strength of the LPF applied during decoding.
        /// </returns>
        public short LowPassFilterStrength { get; }


        /// <summary>
        /// Gets the base sample rate for the profile.
        /// </summary>
        /// 
        /// <returns>
        /// The base sample rate for the profile, which is used as the default playback rate.
        /// </returns>
        public uint BaseSampleRate { get; }

        /// <summary>
        /// Gets the minimum recommended sample rate for the profile.
        /// </summary>
        /// 
        /// <returns>
        /// The minimum sample rate for the profile
        /// if used for ComputerCraft/OpenComputers/Computronics.
        /// Calculated as one-fourth of the base sample rate.
        /// </returns>
        public uint MinSampleRate { get; }

        /// <summary>
        /// Gets the maximum recommended sample rate for the profile.
        /// </summary>
        /// 
        /// <returns>
        /// The maximum sample rate for the profile
        /// if used for ComputerCraft/OpenComputers/Computronics.
        /// Calculated as twice the base sample rate.
        /// </returns>
        public uint MaxSampleRate { get; }


        /// <summary>
        /// Initializes a new DFPWM profile with the specified parameters.
        /// </summary>
        /// 
        /// <param name="responseIncrement">
        /// The response increment value.
        /// </param>
        /// <param name="responseDecrement">
        /// The response decrement value.
        /// </param>
        /// <param name="responsePrecisionBits">
        /// The number of bits for response precision.
        /// </param>
        /// <param name="lowPassFilterStrength">
        /// The strength of the low-pass filter.
        /// </param>
        /// <param name="baseSampleRate">
        /// The base sample rate.
        /// </param>
        private Profile(
            short responseIncrement,
            short responseDecrement,
            byte responsePrecisionBits,
            short lowPassFilterStrength,
            uint baseSampleRate)
        {
            ResponseIncrement = responseIncrement;
            ResponseDecrement = responseDecrement;
            ResponsePrecisionBits = responsePrecisionBits;
            LowPassFilterStrength = lowPassFilterStrength;

            BaseSampleRate = baseSampleRate;
            MinSampleRate = baseSampleRate / 4;
            MaxSampleRate = baseSampleRate * 2;
        }


        /// <summary>
        /// The older DFPWM profile, primarily used for Comutronics on Minecraft 1.7.10.
        /// </summary>
        public static readonly Profile DFPWM = new(7, 20, 8, 100, 0x8000);

        /// <summary>
        /// The newer DFPWM profile. Used by default as it is the most up-to-date.
        /// </summary>
        public static readonly Profile DFPWM1a = new(1, 1, 10, 140, 48000);
    }

}
