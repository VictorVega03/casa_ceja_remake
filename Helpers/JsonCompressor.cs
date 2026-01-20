using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace CasaCejaRemake.Helpers
{
    /// <summary>
    /// Helper para comprimir y descomprimir objetos JSON usando GZip
    /// Usado para campos como PricingData, TicketData en SaleProduct, CreditProduct, etc.
    /// </summary>
    public static class JsonCompressor
    {
        /// <summary>
        /// Comprime un objeto a JSON y luego a bytes con GZip
        /// </summary>
        /// <typeparam name="T">Tipo del objeto a comprimir</typeparam>
        /// <param name="obj">Objeto a comprimir</param>
        /// <returns>Bytes comprimidos con GZip, o null si el objeto es null</returns>
        public static byte[]? Compress<T>(T? obj)
        {
            if (obj == null)
                return null;

            try
            {
                // Serializar a JSON
                string json = JsonConvert.SerializeObject(obj, Formatting.None);
                
                // Convertir a bytes
                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

                // Comprimir con GZip
                using var outputStream = new MemoryStream();
                using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress))
                {
                    gzipStream.Write(jsonBytes, 0, jsonBytes.Length);
                }

                byte[] compressedBytes = outputStream.ToArray();

                #if DEBUG
                // Log para debugging (solo en DEBUG)
                Console.WriteLine($" Comprimido: {jsonBytes.Length} bytes → {compressedBytes.Length} bytes " +
                                $"(Reducción: {100 - (compressedBytes.Length * 100 / jsonBytes.Length)}%)");
                #endif

                return compressedBytes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Error al comprimir: {ex.Message}");
                throw new InvalidOperationException("Error al comprimir datos JSON", ex);
            }
        }

        /// <summary>
        /// Descomprime bytes GZip a JSON y luego a objeto
        /// </summary>
        /// <typeparam name="T">Tipo del objeto a deserializar</typeparam>
        /// <param name="compressedData">Bytes comprimidos con GZip</param>
        /// <returns>Objeto deserializado, o default(T) si los datos son null</returns>
        public static T? Decompress<T>(byte[]? compressedData)
        {
            if (compressedData == null || compressedData.Length == 0)
                return default;

            try
            {
                // Descomprimir con GZip
                using var inputStream = new MemoryStream(compressedData);
                using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
                using var outputStream = new MemoryStream();
                
                gzipStream.CopyTo(outputStream);
                byte[] decompressedBytes = outputStream.ToArray();

                // Convertir bytes a string JSON
                string json = Encoding.UTF8.GetString(decompressedBytes);

                // Deserializar JSON a objeto
                T? obj = JsonConvert.DeserializeObject<T>(json);

                #if DEBUG
                // Log para debugging (solo en DEBUG)
                Console.WriteLine($" Descomprimido: {compressedData.Length} bytes → {decompressedBytes.Length} bytes");
                #endif

                return obj;
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Error al descomprimir: {ex.Message}");
                throw new InvalidOperationException("Error al descomprimir datos JSON", ex);
            }
        }

        /// <summary>
        /// Comprime un objeto a JSON (sin GZip) - útil para campos JSON simples
        /// </summary>
        /// <typeparam name="T">Tipo del objeto</typeparam>
        /// <param name="obj">Objeto a serializar</param>
        /// <returns>String JSON, o null si el objeto es null</returns>
        public static string? ToJson<T>(T? obj)
        {
            if (obj == null)
                return null;

            try
            {
                return JsonConvert.SerializeObject(obj, Formatting.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Error al serializar JSON: {ex.Message}");
                throw new InvalidOperationException("Error al serializar objeto a JSON", ex);
            }
        }

        /// <summary>
        /// Deserializa un string JSON a objeto (sin GZip)
        /// </summary>
        /// <typeparam name="T">Tipo del objeto</typeparam>
        /// <param name="json">String JSON</param>
        /// <returns>Objeto deserializado, o default(T) si el JSON es null/empty</returns>
        public static T? FromJson<T>(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return default;

            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Error al deserializar JSON: {ex.Message}");
                throw new InvalidOperationException("Error al deserializar JSON a objeto", ex);
            }
        }

        /// <summary>
        /// Calcula el tamaño de compresión logrado
        /// </summary>
        /// <param name="originalSize">Tamaño original en bytes</param>
        /// <param name="compressedSize">Tamaño comprimido en bytes</param>
        /// <returns>Porcentaje de reducción</returns>
        public static double GetCompressionRatio(int originalSize, int compressedSize)
        {
            if (originalSize == 0)
                return 0;

            return Math.Round(100.0 - ((double)compressedSize * 100.0 / originalSize), 2);
        }
    }
}