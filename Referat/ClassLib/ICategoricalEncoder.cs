// Интерфейс для всех кодировщиков
namespace ClassLib
{
    public interface ICategoricalEncoder
    {
        string Name { get; }
        EncodingResult EncodeAndEvaluate(List<DataPoint> dataset);
    }
}