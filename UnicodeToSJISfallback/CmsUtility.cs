using System;
using System.Text;

namespace InsidersCMS
{
    public static class CmsUtility
    {

        public static class Encoding
        {
            public static System.Text.Encoding ShiftJISwithReplaceFallback =
                System.Text.Encoding.GetEncoding("Shift_JIS", new EncoderUnicodeToSJisFallback(), DecoderFallback.ReplacementFallback);
        }

        private const int MAX_COUNT_CharacterEntityReference = 10; // Unicode文字実体参照の最大長（ &#x0; ～ &#x10ffff; まで）

        // 入力文字をエンコードできない場合、このフォールバック（＝エラー時の代替処理機構）が使用されるクラス。
        private class EncoderUnicodeToSJisFallback : EncoderFallback
        {
            // エンコーダーのフォールバック バッファーを提供するオブジェクトを生成して返す。
            public override EncoderFallbackBuffer CreateFallbackBuffer()
            {
                return new EncoderUnicodeToSJisFallbackBuffer();
            }

            // エンコードできなかった「入力文字」を置き換える「代替文字列」の最大文字数を返す。
            public override int MaxCharCount
            {
                get { return MAX_COUNT_CharacterEntityReference; } // 具体的には "&#x00a9;" のようなUnicode文字実体参照表記の長ささになる。
            }
        }

        // 入力文字をエンコードできないときに、エンコーダーに代替文字列を返せるようにするためのバッファーとして使われるクラス。
        private class EncoderUnicodeToSJisFallbackBuffer : EncoderFallbackBuffer
        {
            // 代替文字列のバッファー
            private string alternativeCharBuffer;

            // 代替文字列のバッファーにおける現在位置
            private int currentPosition;

            // 代替文字列への変換もできなかった場合のエラー文字（通常はないと思われるが念のため）
            private string giveupString = "★";

            public EncoderUnicodeToSJisFallbackBuffer()
            {
                Reset(); // 状態を初期化する
            }

            // 代替文字列のバッファーで、処理されずに残っている文字数。
            public override int Remaining
            {
                get { return alternativeCharBuffer.Length - currentPosition; }
            }

            // エンコードできないUnicode文字が見つかるとこのメソッドが呼び出されるので、
            // その入力文字を代替文字列に置き換えてバッファーに保存する。
            public override bool Fallback(char charUnknown, int index)
            {
                // charUnknown: 入力文字
                // index:       入力バッファーにおける文字のインデックス位置

                if (currentPosition < alternativeCharBuffer.Length)
                {
                    // ここに来るのは、現在位置が代替文字列バッファーの最後まで到達していない状態
                    throw new ArgumentException("原因がよく分からないけど、" +
                        "代替文字列バッファーの全ての文字が取得されていない状態で、" +
                        "さらに新しいUnicode文字がフォールバックされている。" +
                        "基本的には起こりえないエラーが発生してしまった。");
                }

                // このクラスでは、エンコードできない文字は「Unicode文字実体参照表記」の文字配列に置き換える。
                // ちなみに、Unicodeは1文字あたり2バイト（0x0000～0xFFFF）で、16進数の場合は「&#x+16進数」で表記する。例えば「©」を表すには「&#x00a9;」となる。
                alternativeCharBuffer = String.Format("&#x{0:x};", (int)charUnknown);
                if (alternativeCharBuffer.Length > MAX_COUNT_CharacterEntityReference)
                {
                    alternativeCharBuffer = giveupString;
                }

                currentPosition = 0; // 代替文字列バッファーを作り直したので、現在位置を先頭に初期化する。

                return true; // Unicode文字を処理できる場合は true。できずに無視する場合はfalse。
            }

            // エンコードできないUnicodeサロゲート文字が見つかるとこのメソッドが呼び出されるので、
            // その入力サロゲート文字を代替文字列に置き換えてバッファーに保存する。
            public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
            {
                // charUnknownHigh: 入力ペアの上位サロゲート。最小値＝U+D800、最大値＝0xDBFF。
                // charUnknownLow:  入力ペアの下位サロゲート。最小値＝0xDC00、最大値＝ 0xDFFF。
                // index:           入力バッファーにおけるサロゲートペアのインデックス位置。

                if (currentPosition < alternativeCharBuffer.Length)
                {
                    // ここに来るのは、現在位置が代替文字列バッファーの最後まで到達していない状態
                    throw new ArgumentException("原因がよく分からないけど、" +
                        "代替文字列バッファーの全ての文字が取得されていない状態で、" +
                        "さらに新しいUnicodeサロゲート文字がフォールバックされている。" +
                        "基本的には起こりえないエラーが発生してしまった。");
                }

                // サロゲートペアを単一文字に変換する（ 0x010000 ～ 0x10ffff の範囲になるはず）。
                int surrogateChar = 0x10000
                  + ((int)charUnknownHigh - 0xD800) * 0x400
                  + ((int)charUnknownLow - 0xDC00);
                // validate the range?

                // このクラスでは、エンコードできない文字は「Unicode文字実体参照表記」の文字配列に置き換える。
                // ちなみに、Unicodeサロゲート文字は1文字あたり4バイト（0x010000～0x10ffff）で、16進数の場合は「&#x+16進数」で表記する。例えば「𪚲」を表すには「&#x02a6b2;」となる。
                alternativeCharBuffer = String.Format("&#x{0:x};", (int)surrogateChar);
                if (alternativeCharBuffer.Length > MAX_COUNT_CharacterEntityReference)
                {
                    alternativeCharBuffer = giveupString;
                }

                currentPosition = 0; // 代替文字列バッファーを作り直したので、現在位置を先頭に初期化する。

                return true; // サロゲートペアを処理できる場合は true。できずに無視する場合は false。
            }

            // 代替文字列バッファーにおける次の1文字を取得する。
            // ※代替文字列はこのクラスのバッファーから1文字ずつ取得されながら完成するので、ここは頻繁に呼び出される。
            public override char GetNextChar()
            {
                if (currentPosition >= alternativeCharBuffer.Length)
                {
                    // 現在位置がバッファーの長さに到達しているので、次の文字はない。
                    return (char)0; //「次の文字はない」という意味。
                }

                return alternativeCharBuffer[currentPosition++]; // 代替文字列バッファーの次の開始位置の1文字を返す。
            }

            // 代替文字列バッファーにおける前の文字位置に移動する。
            public override bool MovePrevious()
            {
                if (currentPosition <= 0)
                {
                    return false; // 現在位置が0では、前に移動することは不可能
                }

                currentPosition--; // 前の文字列位置へ移動

                return true; // 前の文字列位置へ移動した場合は true。それ以外の場合は false。
            }

            // フォールバックバッファーに関連するすべてのデータおよびステータス情報を初期化する。
            public override void Reset()
            {
                alternativeCharBuffer = String.Empty;
                currentPosition = 0;

                base.Reset();
            }
        }

    }
}
