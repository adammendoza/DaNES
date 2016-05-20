﻿using Xunit;

namespace DanTup.DaNES.Emulation.Tests.CpuTests
{
	public class LdaTests : CpuTests
	{
		[Theory]
		[InlineData(0, true, false)]
		[InlineData(1, false, false)]
		[InlineData(127, false, false)]
		[InlineData(128, false, true)]
		[InlineData(129, false, true)]
		[InlineData(255, false, true)]
		public void Lda_Immediate(byte value_to_load, bool expectZero, bool expectNegative)
		{
			Run(0xA9, value_to_load);

			Assert.Equal(value_to_load, cpu.Accumulator);
			Assert.Equal(expectZero, cpu.ZeroResult);
			Assert.Equal(expectNegative, cpu.Negative);
		}

		[Theory]
		[InlineData(0, 0, true, false)]
		[InlineData(1, 1, false, false)]
		[InlineData(127, 2, false, false)]
		[InlineData(128, 3, false, true)]
		[InlineData(129, 4, false, true)]
		[InlineData(255, 255, false, true)]
		public void Lda_Zero_Page(byte value_to_load, byte ram_location, bool expectZero, bool expectNegative)
		{
			cpu.Ram.Write(ram_location, value_to_load);

			Run(0xA5, ram_location);

			Assert.Equal(value_to_load, cpu.Accumulator);
			Assert.Equal(expectZero, cpu.ZeroResult);
			Assert.Equal(expectNegative, cpu.Negative);
		}

		[Theory]
		[InlineData(0, 0, 10, true, false)]
		[InlineData(1, 1, 11, false, false)]
		[InlineData(127, 2, 12, false, false)]
		[InlineData(128, 3, 13, false, true)]
		[InlineData(129, 4, 14, false, true)]
		[InlineData(255, 255, 255, false, true)]
		public void Lda_Zero_Page_Offset_X(byte value_to_load, byte ram_location, byte offset, bool expectZero, bool expectNegative)
		{
			cpu.Ram.Write(ram_location + offset, value_to_load);

			cpu.XRegister = offset;
			Run(0xB5, ram_location);

			Assert.Equal(value_to_load, cpu.Accumulator);
			Assert.Equal(expectZero, cpu.ZeroResult);
			Assert.Equal(expectNegative, cpu.Negative);
		}
	}
}
