using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Editor.Builds
{
	internal interface IRestoreableProperty
	{
		void CacheValue();
		void RestoreValue();
	}

	internal class RestoreableProperty<T> : IRestoreableProperty
	{
		private T mDefaultValue;
		private Func<T> mGetter;
		private Action<T> mSetter;

		public RestoreableProperty(Func<T> getter, Action<T> setter)
		{
			if (getter == null) throw new ArgumentException(nameof(getter));
			if (setter == null) throw new ArgumentException(nameof(setter));
			mGetter = getter;
			mSetter = setter;
		}

		public void CacheValue() => mDefaultValue = mGetter.Invoke();
		public void SetValue(T value) => mSetter.Invoke(value);
		public void RestoreValue() => mSetter.Invoke(mDefaultValue);
	}
}
