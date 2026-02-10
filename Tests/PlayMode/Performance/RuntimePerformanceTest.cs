using System.Collections;
using Geuneda.DataExtensions;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;

namespace Geuneda.DataExtensions.Tests.PlayMode.Performance
{
	[TestFixture]
	public class RuntimePerformanceTest
	{
		[UnityTest, Performance]
		public IEnumerator ObservableField_HighFrequencyUpdates_FrameTimeImpact()
		{
			var field = new ObservableField<int>(0);
			field.Observe((p, c) => { /* some work */ });

			yield return Measure.Frames().Run();

			for (int i = 0; i < 1000; i++)
			{
				field.Value = i;
			}
		}
	}
}
