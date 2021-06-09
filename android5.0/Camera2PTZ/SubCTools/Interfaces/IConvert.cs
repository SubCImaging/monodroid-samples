// <copyright file="IConvert.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Interfaces
{
    public interface IPropertyConverter
    {
        string Format { get; }

        bool TryConvert(object data, out object value);

        bool TryConvertBack(object data, out object value);
    }

    public interface IPropertyConverter<T, K> : IPropertyConverter
    {
        bool TryConvert(T data, out K value);

        bool TryConvertBack(K data, out T value);
    }

    public interface IConvert<T>
    {
        bool TryConvert(string data, out T convertedData);
    }

    public interface IConvert
    {
        bool TryConvert(string data, out object convertedData);
    }
}
