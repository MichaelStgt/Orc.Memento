﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MementoServiceFacts.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.Memento.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Catel.Test;
    using Mocks;
    using NUnit.Framework;

    public class MementoServiceFacts
    {
        #region Nested type: TheBeginBatchMethod
        [TestFixture]
        public class TheBeginBatchMethod
        {
            [TestCase]
            public void BeginsNewBatchWhenThereAlreadyIsABatch()
            {
                var mementoService = new MementoService();
                var model = new MockModel();

                var firstBatch = mementoService.BeginBatch("FirstBatch");
                mementoService.Add(new PropertyChangeUndo(model, "Value", model.Value));
                Assert.AreEqual(1, firstBatch.ActionCount);

                var secondBatch = mementoService.BeginBatch("SecondBatch");
                mementoService.Add(new PropertyChangeUndo(model, "Value", model.Value));
                Assert.AreEqual(1, secondBatch.ActionCount);

                // Also check if the first batch was closed
                Assert.AreEqual(1, mementoService.UndoBatches.Count());
                Assert.AreEqual(1, firstBatch.ActionCount);
            }
        }
        #endregion

        #region Nested type: TheConstructor
        [TestFixture]
        public class TheConstructor
        {
            #region Methods
            [TestCase]
            public void ThrowsArgumentOutOfRangeExceptionForNegativeParameter()
            {
                ExceptionTester.CallMethodAndExpectException<ArgumentOutOfRangeException>(() => new MementoService(-1));
            }

            [TestCase]
            public void ExpectDefaultMaximumSupportedActionsValue()
            {
                var mementoService = new MementoService();
                Assert.AreEqual(300, mementoService.MaximumSupportedBatches);
            }
            #endregion
        }
        #endregion

        #region Nested type: TheEndBatchMethod
        [TestFixture]
        public class TheEndBatchMethod
        {
            [TestCase]
            public void EndsBatchWhenThereAlreadyIsABatch()
            {
                var mementoService = new MementoService();
                var model = new MockModel();

                var firstBatch = mementoService.BeginBatch("FirstBatch");
                mementoService.Add(new PropertyChangeUndo(model, "Value", model.Value));
                Assert.AreEqual(1, firstBatch.ActionCount);

                var secondBatch = mementoService.BeginBatch("SecondBatch");
                mementoService.Add(new PropertyChangeUndo(model, "Value", model.Value));
                Assert.AreEqual(1, secondBatch.ActionCount);
                mementoService.EndBatch();

                Assert.AreEqual(2, mementoService.UndoBatches.Count());
            }
        }
        #endregion

        #region Nested type: TheIsEnabledProperty
        [TestFixture]
        public class TheIsEnabledProperty
        {
            [TestCase]
            public void IsTrueByDefault()
            {
                var mementoService = new MementoService();

                Assert.IsTrue(mementoService.IsEnabled);
            }

            [TestCase]
            public void PreventsAdditionsWhenDisabled()
            {
                var mementoService = new MementoService();
                mementoService.IsEnabled = false;

                var undo1 = new MockUndo(true);
                mementoService.Add(undo1);

                Assert.IsFalse(mementoService.CanRedo);
            }
        }
        #endregion

        #region Nested type: TheMaximumSupportedProperty
        [TestFixture]
        public class TheMaximumSupportedProperty
        {
            #region Methods
            [TestCase]
            public void MaximumSupportedOperationsTest()
            {
                var mementoService = new MementoService(5);
                var listUndoOps = new List<MockUndo>();

                for (var i = 0; i < 10; i++)
                {
                    var memento = new MockUndo() {Value = i};
                    mementoService.Add(memento);
                    listUndoOps.Add(memento);
                }

                var count = 0;
                while (mementoService.CanUndo)
                {
                    mementoService.Undo();
                    count++;
                }

                for (var i = 0; i < 5; i++)
                {
                    Assert.IsFalse(listUndoOps[i].UndoCalled);
                }

                for (var i = 5; i < 10; i++)
                {
                    Assert.IsTrue(listUndoOps[i].UndoCalled);
                }

                Assert.AreEqual(count, mementoService.MaximumSupportedBatches);
            }
            #endregion
        }
        #endregion

        #region Nested type: TheRedoMethod
        [TestFixture]
        public class TheRedoMethod
        {
            #region Methods
            [TestCase]
            public void RedoTest()
            {
                var mementoService = new MementoService();
                var undo1 = new MockUndo(true);

                mementoService.Add(undo1);
                mementoService.Undo();
                Assert.IsTrue(undo1.UndoCalled);
                Assert.IsFalse(undo1.RedoCalled);
                Assert.IsTrue(mementoService.CanRedo);

                mementoService.Redo();
                Assert.IsTrue(undo1.RedoCalled);
                Assert.IsFalse(mementoService.CanRedo);
            }

            [TestCase]
            public void HandlesDoubleRedo()
            {
                var obj = new MockModel {Value = "value1"};
                var service = new MementoService();

                service.RegisterObject(obj);
                obj.Value = "value2";
                obj.Value = "value3";

                service.Undo();
                Assert.AreEqual("value2", obj.Value);

                service.Undo();
                Assert.AreEqual("value1", obj.Value);

                service.Redo();
                Assert.AreEqual("value2", obj.Value);

                service.Redo();
                Assert.AreEqual("value3", obj.Value);
            }

            [TestCase]
            public void CanRedoTest()
            {
                var mementoService = new MementoService();
                Assert.IsFalse(mementoService.CanUndo);

                mementoService.Add(new MockUndo());
                Assert.IsTrue(mementoService.CanUndo);

                mementoService.Undo();
                Assert.IsFalse(mementoService.CanRedo);

                mementoService.Add(new MockUndo(true));
                Assert.IsTrue(mementoService.CanUndo);

                mementoService.Undo();
                Assert.IsFalse(mementoService.CanUndo);
                Assert.IsTrue(mementoService.CanRedo);

                mementoService.Redo();
                Assert.IsTrue(mementoService.CanUndo);
                Assert.IsFalse(mementoService.CanRedo);
            }

            [TestCase(1)]
            [TestCase(3)]
            [TestCase(6)]
            public void RaisesUpdatedEvent(int actionsCount)
            {
                var mementoService = new MementoService();
                var raisedEventsCount = 0;
                mementoService.Updated += (sender, args) => 
                {
                    if(args.MementoAction == MementoAction.Redo)
                    {
                        raisedEventsCount++;
                    }
                };

                for (var i = 0; i < actionsCount; i++)
                {
                    mementoService.Add(new MockUndo(true));
                }

                for (var i = 0; i < actionsCount; i++)
                {
                    mementoService.Undo();
                }

                for (var i = 0; i < actionsCount; i++)
                {
                    mementoService.Redo();
                }

                Assert.AreEqual(actionsCount, raisedEventsCount);
            }

            #endregion
        }
        #endregion

        #region Nested type: TheUndoMethod
        [TestFixture]
        public class TheUndoMethod
        {
            #region Methods
            [TestCase]
            public void UndoTest()
            {
                var mementoService = new MementoService();
                var undo1 = new MockUndo();
                var undo2 = new MockUndo();

                mementoService.Add(undo1);
                mementoService.Add(undo2);

                mementoService.Undo();
                Assert.IsTrue(undo2.UndoCalled);
                Assert.IsFalse(undo1.UndoCalled);
                Assert.IsTrue(mementoService.CanUndo);
            }

            [TestCase]
            public void HandlesDoubleUndo()
            {
                var obj = new MockModel {Value = "value1"};
                var service = new MementoService();

                service.RegisterObject(obj);

                obj.Value = "value2";
                obj.Value = "value3";

                service.Undo();
                Assert.AreEqual("value2", obj.Value);

                service.Undo();
                Assert.AreEqual("value1", obj.Value);
            }

            [TestCase]
            public void CanUndoTest()
            {
                var mementoService = new MementoService();
                Assert.IsFalse(mementoService.CanUndo);

                mementoService.Add(new MockUndo());
                Assert.IsTrue(mementoService.CanUndo);

                mementoService.Undo();
                Assert.IsFalse(mementoService.CanUndo);
            }

            [TestCase(1)]
            [TestCase(3)]
            [TestCase(6)]
            public void RaisesUpdatedEvent(int actionsCount)
            {
                var mementoService = new MementoService();
                var raisedEventsCount = 0;
                mementoService.Updated += (sender, args) =>
                {
                    if (args.MementoAction == MementoAction.Undo)
                    {
                        raisedEventsCount++;
                    }
                };

                for (var i = 0; i < actionsCount; i++)
                {
                    mementoService.Add(new MockUndo(true));
                }

                for (var i = 0; i < actionsCount; i++)
                {
                    mementoService.Undo();
                }

                Assert.AreEqual(actionsCount, raisedEventsCount);
            }
            #endregion
        }
        #endregion

        #region Nested type: TheUnregisterObjectMethod
        [TestFixture]
        public class TheUnregisterObjectMethod
        {
            #region Methods
            [TestCase]
            public void ThrowsArgumentNullExceptionForNullInstance()
            {
                var service = new MementoService();

                ExceptionTester.CallMethodAndExpectException<ArgumentNullException>(() => service.UnregisterObject(null));
            }

            [TestCase]
            public void CancelsSubscriptionForInstance()
            {
                var obj = new MockModel {Value = "value1"};
                var service = new MementoService();

                service.RegisterObject(obj);
                service.UnregisterObject(obj);

                obj.Value = "newvalue";

                Assert.IsFalse(service.CanUndo);
            }

            [TestCase]
            public void ClearsCurrentUndoRedoStackForInstance()
            {
                var obj = new MockModel {Value = "value1"};
                var service = new MementoService();

                service.RegisterObject(obj);

                obj.Value = "newvalue1";
                Assert.IsFalse(service.CanRedo);

                service.UnregisterObject(obj);

                Assert.IsFalse(service.CanUndo);
            }
            #endregion
        }
        #endregion
    }
}