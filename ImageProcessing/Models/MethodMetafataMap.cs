namespace ImageProcessing.Models;

internal static class MethodMetafataMap
{
    public static readonly Dictionary<string, MethodMetadata> Map = new()
    {
        ["Niblack"] = new()
        {
            UsesWindow = true,
            ParameterLabel = "k (коэффициент Ниблэка)",
            DefaultParameter = "-0.2",
            Description = "Для каждого пикселя вычисляются средняя яркость и стандартное отклонение в локальном окне. Порог T = m + k·σ. Подходит для изображений с неравномерным освещением."
        },
        ["Sauvola"] = new()
        {
            UsesWindow = true,
            ParameterLabel = "k (коэффициент Сауволы)",
            DefaultParameter = "0.2",
            Description = "Локальный порог T = m·(1 + k·(σ/R − 1)), где R = 128. Метод учитывает локальный контраст и часто хорошо выделяет тёмный текст на светлом фоне."
        },
        ["AdaptiveMean"] = new()
        {
            UsesWindow = true,
            ParameterLabel = "C (смещение порога)",
            DefaultParameter = "7",
            Description = "Порог каждого пикселя равен среднему значению яркости в окне минус C. Размер окна задаёт область соседей; метод компенсирует плавное изменение освещения."
        },
        ["AdaptiveGaussian"] = new()
        {
            UsesWindow = true,
            ParameterLabel = "C (смещение порога)",
            DefaultParameter = "7",
            Description = "Порог строится по взвешенному среднему соседних пикселей: близкие к центру пиксели имеют больший вес по гауссову ядру. Затем из среднего вычитается C."
        },
        ["Point"] = new()
        {
            UsesWindow = false,
            ParameterLabel = "Порог отклика",
            DefaultParameter = "80",
            Description = "Применяется лапласианоподобная маска 3×3: центральный пиксель сравнивается с окружением. Пиксели с модулем отклика выше порога отмечаются белым."
        },
        ["HorizontalLine"] = new()
        {
            UsesWindow = false,
            ParameterLabel = "Порог отклика",
            DefaultParameter = "120",
            Description = "Свертка с маской, усиливающей горизонтальные структуры. Белым отмечаются пиксели, отклик которых по модулю превышает порог."
        },
        ["VerticalLine"] = new()
        {
            UsesWindow = false,
            ParameterLabel = "Порог отклика",
            DefaultParameter = "120",
            Description = "Свертка с маской, усиливающей вертикальные структуры. Белым отмечаются пиксели, отклик которых по модулю превышает порог."
        },
        ["DiagonalDownLine"] = new()
        {
            UsesWindow = false,
            ParameterLabel = "Порог отклика",
            DefaultParameter = "120",
            Description = "Направленная маска выделяет диагональные структуры, идущие сверху-слева вниз-вправо (↘)."
        },
        ["DiagonalUpLine"] = new()
        {
            UsesWindow = false,
            ParameterLabel = "Порог отклика",
            DefaultParameter = "120",
            Description = "Направленная маска выделяет диагональные структуры, идущие снизу-слева вверх-вправо (↙)."
        },
        ["Sobel"] = new()
        {
            UsesWindow = false,
            ParameterLabel = "Порог границы",
            DefaultParameter = "100",
            Description = "Операторы Собеля оценивают горизонтальную и вертикальную производные яркости. Модуль градиента √(Gx² + Gy²) показывает силу перепада; сильные перепады отмечаются белым."
        }
    };
}
