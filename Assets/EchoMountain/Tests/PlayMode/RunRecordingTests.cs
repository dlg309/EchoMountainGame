using EchoMountain.Riding;
using NUnit.Framework;
using UnityEngine;

namespace EchoMountain.Tests
{
    public sealed class RunRecordingTests
    {
        [Test]
        public void SamplesInterpolateMotionPoseAndTimestampedEvents()
        {
            var clip = new RunRecording();
            clip.Begin(MountainLine.Summit, true);
            clip.Add(new ReplayFrame
            {
                time = 0f, position = Vector3.zero, rotation = Quaternion.Euler(0f, 350f, 0f),
                pose = new RiderVisualPose { hipHeight = 0.9f }
            });
            clip.Add(new ReplayFrame
            {
                time = 1f, position = new Vector3(0f, 0f, 10f), rotation = Quaternion.Euler(0f, 10f, 0f),
                velocity = Vector3.forward * 10f, pose = new RiderVisualPose { hipHeight = 0.6f, indy = 1f },
                grab = BoardGrab.Indy, landings = 1, points = 260
            }, true);
            clip.Seal();
            Assert.That(clip.CanReplay, Is.True);
            Assert.That(clip.Course, Is.EqualTo(LocalRunStore.Course));
            Assert.That(clip.Assist, Is.EqualTo(LocalRunStore.Assist));
            Assert.That(clip.Practice, Is.True);
            var middle = clip.Sample(0.5f);
            Assert.That(middle.position.z, Is.EqualTo(5f).Within(0.001f));
            Assert.That(Quaternion.Angle(middle.rotation, Quaternion.identity), Is.LessThan(0.01f), "Interpolate across heading wrap.");
            Assert.That(middle.pose.hipHeight, Is.EqualTo(0.75f).Within(0.001f));
            Assert.That(middle.pose.indy, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(middle.points, Is.Zero, "Future landing events must not appear early.");
            Assert.That(clip.Sample(10f).points, Is.EqualTo(260));
            Assert.That(clip.Sample(-10f).time, Is.Zero);
            Assert.That(clip.Sample(float.NaN).time, Is.Zero);
        }

        [Test]
        public void RecordingIsBoundedAndRejectsInvalidMotion()
        {
            var clip = new RunRecording();
            clip.Begin(MountainLine.Pine, false);
            for (int i = 0; i <= 180 * 60; i++)
                clip.Add(new ReplayFrame { time = i / 60f, rotation = Quaternion.identity });
            clip.Seal();
            Assert.That(clip.CanReplay, Is.True);
            Assert.That(clip.Count, Is.LessThanOrEqualTo(RunRecording.MaximumFrames));
            clip.Begin(MountainLine.Pine, false);
            clip.Add(new ReplayFrame { time = 0f, rotation = Quaternion.identity });
            clip.Add(new ReplayFrame { time = 181f, rotation = Quaternion.identity });
            clip.Seal();
            Assert.That(clip.CanReplay, Is.False, "A partial overlong run must not be offered as complete.");
            clip.Begin(MountainLine.Pine, false);
            clip.Add(new ReplayFrame { time = 0f, rotation = Quaternion.identity });
            clip.Add(new ReplayFrame { time = 1f, rotation = Quaternion.identity, position = new Vector3(float.NaN, 0f, 0f) });
            clip.Seal();
            Assert.That(clip.CanReplay, Is.False);
        }
    }
}
