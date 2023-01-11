﻿
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Runtime.InteropServices
Imports NewTek
Imports NewTek.NDI
Public Class Form1
    Shared audioNumSamples As Integer() = {1602, 1601, 1602, 1601, 1602}


    ' Note that I employ 'Using' statements to safely dispose of my IDisposable objects.
    ' You can manually handle .Dispose() for longer lived objects or use any pattern you prefer.

    ' this will show up as a source named "VB.Net Example" with all other settings at their defaults
    Dim sendInstance As New Sender("VB.Net Example")

    ' We are going to create a 1920x1080 16:9 frame at 29.97Hz, progressive (default).
    Dim videoFrame As New VideoFrame(1920, 1080, (16.0F / 9.0F), 30000, 1001)

    ' We are also going to create an audio frame with enough for 1700 samples for a bit of safety,
    ' but 1602 should be enough using our settings as long as we don't overrun the buffer.
    ' 48khz, stereo in the example.
    Dim audioFrame As New AudioFrame(1700, 48000, 2)

    ' get a compatible bitmap and graphics context
    Dim bmp As New Bitmap(videoFrame.Width, videoFrame.Height, videoFrame.Stride, System.Drawing.Imaging.PixelFormat.Format32bppPArgb, videoFrame.BufferPtr)

    Dim graphics As Graphics = Graphics.FromImage(bmp)


    ' We'll use these later inside the loop
    Dim textFormat As New StringFormat()



    Dim fontFamily As New FontFamily("Arial")
    Dim outlinePen As New Pen(Color.Black, 2.0F)
    Dim thinOutlinePen As New Pen(Color.Black, 1.0F)

    Private Shared Sub FillAudioBuffer(audioFrame As AudioFrame, doTone As Boolean)
        ' should never happen
        If audioFrame.AudioBuffer = IntPtr.Zero Then
            Return
        End If

        ' temp space for floats
        Dim floatBuffer As Single() = New Single(CInt(audioFrame.NumSamples) - 1) {}

        ' make the tone or silence
        Dim cycleLength As Double = CDbl(audioFrame.SampleRate) / 1000.0
        Dim sampleNumber As Integer = 0
        For i As Integer = 0 To CInt(audioFrame.NumSamples) - 1
            Dim time As Double = System.Math.Max(System.Threading.Interlocked.Increment(sampleNumber), sampleNumber - 1) / cycleLength
            floatBuffer(i) = If(doTone, CSng(Math.Sin(2.0F * Math.PI * time) * 0.1), 0.0F)
        Next

        ' fill each channel with our floats...
        For ch As Integer = 0 To CInt(audioFrame.NumChannels) - 1
            ' scary pointer math ahead...
            ' where does this channel start in the unmanaged buffer?
            Dim destStart As New IntPtr(audioFrame.AudioBuffer.ToInt64() + (ch * audioFrame.ChannelStride))

            ' copy the float array into the channel
            Marshal.Copy(floatBuffer, 0, destStart, audioFrame.NumSamples)
        Next
    End Sub
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub
    Private Shared Sub DrawPrettyText(graphics As Graphics, text As [String], size As Single, family As FontFamily, origin As Point, format As StringFormat,
     fill As Brush, outline As Pen)
        ' make a text path
        Dim path As New GraphicsPath()
        path.AddString(text, family, 0, size, origin, format)

        ' Draw the pretty text
        graphics.FillPath(fill, path)
        graphics.DrawPath(outline, path)
    End Sub

    Public Sub makeready()
        textFormat.Alignment = StringAlignment.Near
        textFormat.LineAlignment = StringAlignment.Near
        graphics.SmoothingMode = SmoothingMode.AntiAlias
    End Sub
    Public Sub Main()

        makeready()

        ' We will send 10000 frames of video.
        'For frameNumber As Integer = 0 To 9999
        ' are we connected to anyone?
        If sendInstance.GetConnections(10000) < 1 Then
                ' no point rendering
                Console.WriteLine("No current connections, so no rendering needed.")

                ' Wait a bit, otherwise our limited example will end before you can connect to it
                System.Threading.Thread.Sleep(50)
            Else
            ' Because we are clocking to the video it is better to always submit the audio
            ' before, although there is very little in it. I'll leave it as an excercise for the
            ' reader to work out why.
            'audioFrame.NumSamples = audioNumSamples(frameNumber Mod 5)
            'audioFrame.ChannelStride = audioFrame.NumSamples * 4

            '' put tone in it every 30 frames
            'Dim doTone As Boolean = frameNumber Mod 30 = 0
            'FillAudioBuffer(audioFrame, doTone)

            ' Submit the audio buffer
            sendInstance.Send(audioFrame)

            ' fill it with a lovely color
            graphics.Clear(Color.Red)

            ' show which source we are
            DrawPrettyText(graphics, "VB Example Source by mcs", 96.0F, fontFamily, New Point(0, 100), textFormat,
                                        Brushes.White, outlinePen)

            ' Get the tally state of this source (we poll it),
            ' This gets a snapshot of the current tally state.
            ' Accessing sendInstance.Tally directly would make an API call
            ' for each "if" below and could cause inaccurate results.
            Dim NDI_tally As NDIlib.tally_t
                NDI_tally = sendInstance.Tally

                ' Do something different depending on where we are shown
                If NDI_tally.on_program Then
                DrawPrettyText(graphics, "On Program", 96.0F, fontFamily, New Point(0, 225), textFormat,
                                            Brushes.White, outlinePen)
            ElseIf NDI_tally.on_preview Then
                DrawPrettyText(graphics, "On Preview", 96.0F, fontFamily, New Point(0, 225), textFormat,
                                            Brushes.White, outlinePen)
            End If

            ' show what frame we've rendered
            'DrawPrettyText(graphics, [String].Format("Frame {0}", frameNumber.ToString()), 96.0F, fontFamily, New Point(960, 350), textFormat, Brushes.White, outlinePen)


            ' show current time
            DrawPrettyText(graphics, System.DateTime.Now.ToString(), 96.0F, fontFamily, New Point(0, 900), textFormat,
                                        Brushes.White, outlinePen)

            ' We now submit the frame. Note that this call will be clocked so that we end up submitting 
            ' at exactly 29.97fps.
            sendInstance.Send(videoFrame)

                ' Just display something helpful in the console
                'Console.WriteLine("Frame number {0} sent.", frameNumber)
            End If
        ' Next

    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Main()
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        ' fill it with a lovely color
        graphics.Clear(Color.Transparent)
        DrawPrettyText(graphics, ComboBox1.Text, 96.0F, fontFamily, New Point(100, 300), textFormat, Brushes.White, outlinePen)
        sendInstance.Send(videoFrame)

    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        graphics.Clear(Color.Transparent)
        'Graphics.FromImage(Image.FromFile("d:/africa.jpg"))
        graphics.DrawImage(Image.FromFile("d:/africa.jpg"), New Point(500, 300))
        DrawPrettyText(graphics, ComboBox1.Text, 96.0F, fontFamily, New Point(500, 600), textFormat, Brushes.White, outlinePen)
        sendInstance.Send(videoFrame)
    End Sub
End Class
