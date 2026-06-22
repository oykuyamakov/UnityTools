using System.Text;
using UnityEngine;

namespace CadiKazani.Scripts.Utility
{
	public static class StringBuilderPool
	{
		private static StringBuilder stringBuilder;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
		private static void Initialize()
		{
			stringBuilder = new StringBuilder();
		}

		public static StringBuilder Get()
		{
			if (stringBuilder == null)
			{
				stringBuilder = new StringBuilder();
			}

			stringBuilder.Clear();
			return stringBuilder;
		}
	}
}
