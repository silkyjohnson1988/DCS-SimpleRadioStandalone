namespace Ciribob.DCS.SimpleRadio.Standalone.Common.Tests
{
    [TestClass()]
    public class ConversionHelpersTests
    {
        [TestMethod()]
        public void ConversionTestByteShortArray()
        {
            byte[] bytes = new byte[] { 255, 253, 102, 0, 5, 0, 0, 0 };

            var shorts = ConversionHelpers.ByteArrayToShortArray(bytes);

            var result = ConversionHelpers.ShortArrayToByteArray(shorts);

            var query = bytes.Where((b, i) => b == result[i]);

            Assert.AreEqual(bytes.Length, query.Count());
        }

        [TestMethod()]
        public void ConversionTestShortByteArray()
        {
            short[] shorts = new short[] { 1, short.MaxValue, short.MinValue, 0 };

            var bytes = ConversionHelpers.ShortArrayToByteArray(shorts);

            var result = ConversionHelpers.ByteArrayToShortArray(bytes);

            var query = shorts.Where((b, i) => b == result[i]);

            Assert.AreEqual(shorts.Length, query.Count());
        }

        [TestMethod()]
        public void ConversionShortToBytes()
        {
            short[] shorts = new short[] { 1, short.MaxValue, short.MinValue, 0 };

            foreach (var shortTest in shorts)
            {
                byte byte1;
                byte byte2;
                ConversionHelpers.FromShort(shortTest, out byte1, out byte2);

                byte[] converted = BitConverter.GetBytes(shortTest);

                Assert.AreEqual(byte1, converted[0]);
                Assert.AreEqual(byte2, converted[1]);

                //convert back
                Assert.AreEqual(shortTest, ConversionHelpers.ToShort(byte1, byte2));
            }
        }

        [TestMethod()]
        public void ConversionTestByteShortArray_WithEmptyArray()
        {
            byte[] bytes = new byte[0];

            var shorts = ConversionHelpers.ByteArrayToShortArray(bytes);

            Assert.AreEqual(0, shorts.Length);
        }

        [TestMethod()]
        public void ConversionTestShortByteArray_WithEmptyArray()
        {
            short[] shorts = new short[0];

            var bytes = ConversionHelpers.ShortArrayToByteArray(shorts);

            Assert.AreEqual(0, bytes.Length);
        }
    }
}