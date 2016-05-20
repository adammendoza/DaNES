﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace DanTup.DaNES.Emulation.Tests.CpuTests
{
	public class NesTest
	{
		const string ExpectedLogFile = "../../NesTest/expected.txt";
		const string RomFile = "../../NesTest/nestest.nes";
		LoggingCpu cpu = new LoggingCpu();

		[Fact]
		public void RunNesTest()
		{
			// Since we'll always throw an exception while there are unhandled opcodes, we'll catch the
			// exception and worry about it only if the implemented opcodes are fine. Otherwise, consider
			// the incorrect opcode the failure.
			var ex = TryRunNesTest();

			var actualLog = cpu.Log.ToString().Split('\n');
			var expectedLog = File.ReadAllLines(ExpectedLogFile);

			CompareResults(ex, actualLog, expectedLog);

			// Logs are in the following form:
			//
			// C000  4C F5 C5 A:00 X: 00 Y: 00 P: 24 SP: FD CYC:  0 SL: 241
			//
			// Logged data is taken prior to the step execution, so an LDA instruction
			// will have the existing Accumulator value in that row.
			// 
			// C000        The program counter (where we're about to execute)
			// 4C          The instruction byte being read
			// F5          Additional data for the isntruction (optional)
			// C5          Additional data for the isntruction (optional)
			// A:00        The accumulator
			// X: 00       The x index register
			// Y: 00       The y index register
			// P: 24       The status flags (booleans) as an INT
			// SP: FD      The stack pointer
			// CYC:  0     How many cycles have executed
			// SL: 241     The current scan line
		}

		// Everything below here is test plumbing for execution and comparison and not test code.

		static void CompareResults(Exception ex, string[] actualLog, string[] expectedLog)
		{
			// Compare all executed insructions.
			for (var i = 0; i < Math.Min(actualLog.Length, expectedLog.Length); i++)
				CompareInstructionLog(actualLog, expectedLog, i);

			// Check log file was complete.
			if (actualLog.Length < expectedLog.Length)
				throw new Exception(string.Format(
					"Unexpected EOF in actual log ({0} lines). Expected log ({1} lines) continues with:\r\n\t{2}",
					actualLog.Length,
					expectedLog.Length,
					expectedLog[actualLog.Length]
				));

			// Check we didn't do too much.
			if (actualLog.Length > expectedLog.Length)
				throw new Exception("Actual log was longer than expected log. Actual log additionally contains:\r\n\t" + actualLog[expectedLog.Length]);

			// Check whether we failed for some other reason.
			if (ex != null)
				throw new Exception("Exception during execution", ex);
		}

		static void CompareInstructionLog(string[] actualLog, string[] expectedLog, int i)
		{
			// We don't currently have the middle part of this log (it's really just a repeat of the first few bytes)
			// so just strip it out.
			// TODO: Uncomment to check the bits at the end!
			var actual = actualLog[i].Substring(0, 16);// + actualLog[i].Substring(48);
			var expected = expectedLog[i].Substring(0, 16);// + expectedLog[i].Substring(48);

			// Compare this line of the log to the known-good NesTest output.
			if (actual != expected)
				throw new Exception(string.Format("Instruction not processed correctly at line {0}:\r\n\r\n{1} (expected)\r\n{2} (actual)\r\r{3} (previous)", i, expected, actual, i > 0 ? expectedLog[i - 1] : ""));
		}

		/// <summary>
		/// Runs the program, but catches and returns any exception so that
		/// we can test the opcodes that did execute before failing due to exceptions.
		/// </summary>
		Exception TryRunNesTest()
		{
			var program = new ArraySegment<byte>(File.ReadAllBytes(RomFile), 0x0010, 0x4000).ToArray();

			try
			{
				cpu.LoadProgram(program, false);
				cpu.Run();
				return null;
			}
			catch (Exception x)
			{
				return x;
			}
		}

		class LoggingCpu : Cpu
		{
			public StringBuilder Log = new StringBuilder();
			LogInfo logInfo;

			class LogInfo
			{
				public int ProgramCounter;
				public List<byte> BytesRead = new List<byte>();
				public string Registers = "";

				public override string ToString()
				{

					return string.Format(
						"{0}{1}{2}{3}",
						ProgramCounter.ToString("X2").PadRight(6),
						string.Join(" ", BytesRead.Select(b => b.ToString("X2"))).PadRight(10),
						"".PadRight(32), // TODO: Don't bother with this for now, it's kinda complicated :)
						Registers
					);
				}
			}

			public override bool ProcessNextOpCode()
			{
				logInfo = new LogInfo();
				logInfo.ProgramCounter = ProgramCounter;
				var status = Convert.ToInt32(string.Format("{0}{1}1{2}{3}{4}{5}{6}", Negative ? 1 : 0, Overflow ? 1 : 0, Interrupted ? 1 : 0, DecimalMode ? 1 : 0, InterruptsEnabled ? 0 : 1, ZeroResult ? 1 : 0, Carry ? 1 : 0), 2);
				logInfo.Registers = string.Format("A:{0:X2} X:{1:X2} Y:{2:X2} P:{3:X2} SP:{4:X2} CYC:{5:X2} SL:{6}", Accumulator, XRegister, YRegister, status, StackPointer, "0".PadLeft(3), "0".PadRight(3));

				try
				{
					return base.ProcessNextOpCode();
				}
				finally
				{
					Log.AppendLine(logInfo.ToString());
				}
			}

			protected override byte ReadNext()
			{
				var result = base.ReadNext();

				logInfo.BytesRead.Add(result);

				return result;
			}
		}
	}
}