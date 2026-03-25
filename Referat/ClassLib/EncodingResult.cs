using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLib
{
    // Класс для хранения результатов кодирования
    public class EncodingResult
    {
        public string MethodName { get; set; }
        public int Dimensionality { get; set; }
        public double QualityScore { get; set; }
        public double EncodingTime { get; set; }
        public double InformationLoss { get; set; }
        public Dictionary<string, double> Metrics { get; set; }

        public override string ToString()
        {
            return $"Метод: {MethodName}\n" +
                   $"Размерность: {Dimensionality}\n" +
                   $"Качество кодирования: {QualityScore:F2}\n" +
                   $"Время кодирования: {EncodingTime:F2} мс\n" +
                   $"Потеря информации: {InformationLoss:F2}%";
        }
    }
}
