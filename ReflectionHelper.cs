using System;
using System.Reflection;

namespace Recruiter
{
	public static class ReflectionHelper
	{
		internal static object Call(this object o, string methodName, params object[] args)
		{
			MethodInfo method = o.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
			bool flag = method != null;
			if (flag)
			{
				try
				{
					return method.Invoke(o, args);
				}
				catch (Exception)
				{
				}
			}
			return null;
		}

		internal static object GetField(this object o, string fieldName)
		{
			FieldInfo field = o.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
			bool flag = field != null;
			if (flag)
			{
				try
				{
					return field.GetValue(o);
				}
				catch (Exception)
				{
				}
			}
			return null;
		}
	}
}
