using System;
using Bootstrapper;
using Leaderboard;
using NUnit.Framework;

namespace LeaderboardTests
{
    public class RunTrackingTests
    {
        [Test]
        public void HeightUsesSpawnOffsetAndRetainsPeakAfterFalling()
        {
            var run = new RunScore(100);
            run.Observe(99);
            Assert.AreEqual(0, run.HeightUnits);
            run.Observe(112.125);
            run.Observe(103);
            Assert.AreEqual(1212, run.HeightUnits);
        }

        [Test]
        public void DeathCapturesFinalPositionAndCannotBeFinalizedTwice()
        {
            var run = new RunScore(-10);
            run.Observe(0);
            Assert.IsTrue(run.Finish(5));
            Assert.AreEqual(1500, run.HeightUnits);
            Assert.IsFalse(run.Finish(200));
            run.Observe(300);
            Assert.AreEqual(1500, run.HeightUnits);
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        [TestCase(double.MaxValue)]
        public void InvalidCoordinatesInvalidateResultWithoutOverflow(double value)
        {
            var run = new RunScore(0);
            run.Observe(10);
            run.Observe(value);
            run.Finish(11);
            Assert.IsFalse(run.IsValid);
            Assert.AreEqual(1000, run.HeightUnits);
        }

        [Test]
        public void VersionsRulesAndSeedsHaveIndependentRecords()
        {
            var history = new LocalRunHistory();
            var a = Result(1000);
            var version = Result(200);
            version.gameVersion = "1.2.0";
            var rules = Result(300);
            rules.rulesetId = "different-rules";
            var seed = Result(400);
            seed.seed++;
            foreach (var result in new[] { a, version, rules, seed })
                Assert.IsTrue(history.Record(result));
            Assert.AreEqual(1000, history.BestHeight(a));
            Assert.AreEqual(200, history.BestHeight(version));
            Assert.AreEqual(300, history.BestHeight(rules));
            Assert.AreEqual(400, history.BestHeight(seed));
        }

        [Test]
        public void EqualAndWorseRunsDoNotReplaceRecord()
        {
            var history = new LocalRunHistory();
            var first = Result(500);
            history.Record(first);
            Assert.IsFalse(history.Record(first));
            Assert.IsFalse(history.Record(Result(500)));
            Assert.IsFalse(history.Record(Result(499)));
            Assert.AreEqual(first.runId, history.bestResults[0].runId);
            Assert.IsTrue(history.Record(Result(501)));
            Assert.AreEqual(1, history.bestResults.Count);
            Assert.AreEqual(501, history.BestHeight(first));
        }

        [Test]
        public void DiagnosticInvalidAndAbandonedRunsNeverBecomePersonalBest()
        {
            var history = new LocalRunHistory();
            var result = Result(10000);
            result.diagnostic = true;
            Assert.IsFalse(history.Record(result));
            result.diagnostic = false;
            result.valid = false;
            Assert.IsFalse(history.Record(result));
            result.valid = true;
            result.endReason = RunEndReason.SceneExit;
            Assert.IsFalse(history.Record(result));
            result.endReason = RunEndReason.ApplicationQuit;
            Assert.IsFalse(history.Record(result));
            Assert.IsEmpty(history.bestResults);
        }

        [Test]
        public void HistoryOwnsACopyOfTheResult()
        {
            var history = new LocalRunHistory();
            var result = Result(125);
            history.Record(result);
            result.heightUnits = 999;
            Assert.AreEqual(125, history.BestHeight(result));
        }

        [Test]
        public void CheckpointsDetectTeleportHealthAndUpgradeOrderChanges()
        {
            var expected = Checkpoint();
            var actual = Checkpoint();
            Assert.IsNull(expected.Difference(actual));
            actual.y += 10;
            Assert.AreEqual("position", expected.Difference(actual));
            actual = Checkpoint();
            actual.hp++;
            Assert.AreEqual("health/alive", expected.Difference(actual));
            actual = Checkpoint();
            actual.upgrades = new[] { 2, 1 };
            Assert.AreEqual("upgrades/order", expected.Difference(actual));
        }

        [Test]
        public void CheckpointsRejectNaNAndDetectCooldownBypass()
        {
            var expected = Checkpoint();
            var actual = Checkpoint();
            actual.vx = float.NaN;
            Assert.AreEqual("invalid checkpoint", expected.Difference(actual));
            actual = Checkpoint();
            actual.dashCooldown = 0;
            Assert.AreEqual("movement state", expected.Difference(actual));
        }

        [Test]
        public void CheckpointsAllowAngleWrappingAndSmallNumericalError()
        {
            var expected = Checkpoint();
            var actual = Checkpoint();
            expected.rotation = 360;
            actual.x += 0.0001f;
            Assert.IsNull(expected.Difference(actual));
        }

        [Test]
        public void MalformedInputIsRejected()
        {
            Assert.IsFalse(new RunInput { kind = (RunInputKind)100 }.IsValid);
            Assert.IsFalse(new RunInput { kind = RunInputKind.Shot, x = float.PositiveInfinity }.IsValid);
            Assert.IsTrue(new RunInput { kind = RunInputKind.Look, x = -1 }.IsValid);
        }

        [Test]
        public void TrainingCannotCarryHealthUpgradesOrStageIntoOpenWorld()
        {
            var session = new GameSessionState(4);
            session.StartTraining();
            session.Current.Hp = 7;
            session.Current.UpgradeIds.Add(3);
            session.CompleteTrainingStage(3);
            session.StartOpenWorld();
            Assert.AreEqual(GameMode.OpenWorld, session.Current.Mode);
            Assert.AreEqual(4, session.Current.Hp);
            Assert.IsEmpty(session.Current.UpgradeIds);
            Assert.AreEqual(0, session.Current.TrainingStage);
            Assert.IsFalse(session.TrainingStagePending);
        }

        [Test]
        public void EachTrainingLessonHasFreshResourcesAndTheCourseEnds()
        {
            var session = new GameSessionState(4);
            session.StartTraining();
            session.Current.Hp = 1;
            session.Current.UpgradeIds.Add(2);
            Assert.IsTrue(session.CompleteTrainingStage(2));
            Assert.IsFalse(session.CompleteTrainingStage(2));
            session.StartTraining();
            Assert.AreEqual(1, session.Current.TrainingStage);
            Assert.AreEqual(4, session.Current.Hp);
            Assert.IsEmpty(session.Current.UpgradeIds);
            Assert.IsFalse(session.CompleteTrainingStage(2));
            Assert.IsTrue(session.TrainingCompleted);
            Assert.AreEqual(GameMode.None, session.Current.Mode);
        }

        [Test]
        public void ExitOrDeathDiscardsRunAndTrainingRestartsFromFirstLesson()
        {
            var session = new GameSessionState(4);
            session.StartTraining();
            session.CompleteTrainingStage(3);
            session.End();
            session.StartTraining();
            Assert.AreEqual(0, session.Current.TrainingStage);
            session.StartOpenWorld();
            var previous = session.Current;
            previous.Hp = 1;
            previous.UpgradeIds.Add(4);
            session.End();
            session.StartOpenWorld();
            previous.UpgradeIds.Add(5);
            Assert.AreEqual(4, session.Current.Hp);
            Assert.IsEmpty(session.Current.UpgradeIds);
        }

        [Test]
        public void OpenWorldCannotAdvanceTraining()
        {
            var session = new GameSessionState(4);
            session.StartOpenWorld();
            Assert.IsFalse(session.CompleteTrainingStage(3));
            Assert.AreEqual(GameMode.OpenWorld, session.Current.Mode);
        }

        private static LocalRunResult Result(long height) => new LocalRunResult
        {
            runId = Guid.NewGuid().ToString("N"), gameVersion = "1.1.0",
            rulesetId = RunScore.RulesetId, seed = 1337, valid = true,
            endReason = RunEndReason.Death, heightUnits = height
        };

        private static RunCheckpoint Checkpoint() => new RunCheckpoint
        {
            x = 1, y = 2, hp = 4, alive = true, dashCooldown = 1.5f, upgrades = new[] { 1, 2 }
        };
    }
}
