// Copyright (C) 2017  Kazuya Ujihara
// This file is under LGPL-2.1 

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace NCDK
{
    internal class ServiceLoader<T> : IEnumerable<T> 
    {
        List<Type> types = new List<Type>();

        private ServiceLoader()
        { }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var type in types)
            {
                bool succeed = false;
                T o = default(T);
                try
                {
                    o = (T)type.GetConstructor(Type.EmptyTypes).Invoke(Array.Empty<object>());
                    succeed = true;
                }
                catch (Exception)
                { }
                if (succeed)
                    yield return o;
            }
            yield break;
        }

        public static ServiceLoader<T> Load()
        {
            var loader = new ServiceLoader<T>();

            using (var srm = typeof(T).Assembly.GetManifestResourceStream(typeof(T).FullName))
            using (var reader = new StreamReader(srm))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (string.IsNullOrEmpty(line) || line[0] == '#')
                        continue;
                    try
                    {
                        var type = typeof(T).Assembly.GetType(line);
                        if (type == null)
                            continue;
                        loader.types.Add(type);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            return loader;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
