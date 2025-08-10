namespace ProtocolCreator.Core;

public static class MathExtension
{

    public static double[] Arrange(double start, double stop, double step, bool stopIncluded = false)
    {
        if (step == 0)
            throw new ArgumentException("Step cannot be zero.", nameof(step));

        // Ensure the direction of step matches the range direction
        if ((step > 0 && start > stop) || (step < 0 && start < stop))
            throw new ArgumentException("Step sign does not match the direction from start to stop.");

        int count;
        if (step > 0)
            count = (int)Math.Ceiling((stop - start) / step);
        else
            count = (int)Math.Ceiling((start - stop) / -step);

        if (stopIncluded)
        {
            var lastValue = start + step * (count - 1);
            if ((step > 0 && lastValue < stop) || (step < 0 && lastValue > stop) || Math.Abs(lastValue - stop) > double.Epsilon)
            {
                count += 1;
            }
        }

        if (count <= 0)
            return [];

        var result = new double[count];
        var value = start;
        for (var i = 0; i < count; i++)
        {
            result[i] = value;
            value += step;
        }

        // If stopIncluded is true, ensure the last value is exactly stop
        if (stopIncluded && count > 0)
            result[^1] = stop;

        return result;
    }

    public static double[] LinearSpace(double start, double stop, int num)
    {
        if (num < 2)
            throw new ArgumentException("num must be at least 2.", nameof(num));

        var result = new double[num];
        var step = (stop - start) / (num - 1);
        for (var i = 0; i < num; i++)
        {
            result[i] = start + step * i;
        }
        // Ensure last value is exactly stop to avoid floating point error
        result[num - 1] = stop;
        return result;
    }

}