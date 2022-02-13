// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class Class1
    {
        [Fact]
        public void MutabilityDoubleTest()
        {
            var engine = new RecalcEngine();
            engine.AddFunction(new AssertDoubleFunction());
            engine.AddFunction(new Set2Function());

            var d = new Dictionary<string, FormulaValue>
            {
                ["prop"] = FormulaValue.New(123)
            };

            var obj = MutableObject.New(d);
            engine.UpdateVariable("obj", obj);

            var expr = @"
AssertDouble(obj.prop, 123);
Set2(obj, ""prop"", 456);
AssertDouble(obj.prop, 456);";

            var x = engine.Eval(expr); // Assert failures will throw.
        }

        [Fact]
        public void MutabilityStringTest()
        {
            var engine = new RecalcEngine();
            engine.AddFunction(new AssertStringFunction());
            engine.AddFunction(new Set2Function());

            var d = new Dictionary<string, FormulaValue>
            {
                ["prop"] = FormulaValue.New("A")
            };

            var obj = MutableObject.New(d);
            engine.UpdateVariable("obj", obj);

            var expr = @"
AssertString(obj.prop, ""A"")
";

            var x = engine.Eval(expr); // Assert failures will throw.
        }

        [Fact]
        public void MutabilityChangeTest()
        {
            var engine = new RecalcEngine();
            engine.AddFunction(new AssertStringFunction());
            engine.AddFunction(new Set2Function());

            var d = new Dictionary<string, FormulaValue>
            {
                ["prop"] = FormulaValue.New("A")
            };

            var obj = MutableObject.New(d);
            string? changed = null;
            ((MutableObject)obj.Impl).PropertyChanged += (o, e) => changed = e.PropertyName;
            engine.UpdateVariable("obj", obj);

            var expr = @"
Set2(obj, ""prop"", ""B"")
";

            var x = engine.Eval(expr); // Assert failures will throw.

            Assert.Equal("prop", changed);
        }

        public class AssertDoubleFunction : ReflectionFunction
        {
            public AssertDoubleFunction()
                : base("AssertDouble", FormulaType.Blank, new UntypedObjectType(), FormulaType.Number)
            {
            }

            public void Execute(UntypedObjectValue obj, NumberValue val)
            {
                var impl = obj.Impl;
                var actual = impl.GetDouble();
                var expected = val.Value;

                if (actual != expected)
                {
                    throw new InvalidOperationException($"Mismatch");
                }
            }
        }

        public class AssertStringFunction : ReflectionFunction
        {
            public AssertStringFunction()
                : base("AssertString", FormulaType.Blank, new UntypedObjectType(), FormulaType.String)
            {
            }

            public void Execute(UntypedObjectValue obj, StringValue val)
            {
                var impl = obj.Impl;
                var actual = impl.GetString();
                var expected = val.Value;

                if (actual != expected)
                {
                    throw new InvalidOperationException($"Mismatch expected {expected} got {actual}");
                }
            }
        }

        public class Set2Function : ReflectionFunction
        {
            public Set2Function()
                : base(
                      "Set2", 
                      FormulaType.Blank,  // returns
                      new UntypedObjectType(), 
                      FormulaType.String, 
                      FormulaType.Number) // $$$ Any?
            {
            }

            public void Execute(UntypedObjectValue obj, StringValue propName, FormulaValue val)
            {
                var impl = (MutableObject)obj.Impl;
                impl.Set(propName.Value, val);
            }
        }

        public class SimpleObject : IUntypedObject
        {
            private readonly FormulaValue _value;

            public SimpleObject(FormulaValue value)
            {
                _value = value;
            }

            public IUntypedObject this[int index] => throw new NotImplementedException();

            public FormulaType Type => _value.Type;

            public int GetArrayLength()
            {
                throw new NotImplementedException();
            }

            public bool GetBoolean()
            {
                return ((BooleanValue)_value).Value;
            }

            public double GetDouble()
            {
                return ((NumberValue)_value).Value;
            }

            public string GetString()
            {
                return ((StringValue)_value).Value;
            }

            public bool TryGetProperty(string value, out IUntypedObject result)
            {
                throw new NotImplementedException();
            }
        }

        public class MutableObject : IUntypedObject, INotifyPropertyChanged
        {
            private Dictionary<string, FormulaValue> _values = new Dictionary<string, FormulaValue>();

            public event PropertyChangedEventHandler PropertyChanged;

            public void Set(string property, FormulaValue newValue)
            {
                _values[property] = newValue;
                if (PropertyChanged != null) 
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(property));
                }
            }

            public static UntypedObjectValue New(Dictionary<string, FormulaValue> d)
            {
                var x = new MutableObject()
                {
                    _values = d
                };

                return new UntypedObjectValue(
                    IRContext.NotInSource(new UntypedObjectType()), 
                    x);
            }

            public IUntypedObject this[int index] => throw new NotImplementedException();

            public FormulaType Type => ExternalType.ObjectType;

            public int GetArrayLength()
            {
                throw new NotImplementedException();
            }

            public bool GetBoolean()
            {
                throw new NotImplementedException();
            }

            public double GetDouble()
            {
                throw new NotImplementedException();
            }

            public string GetString()
            {
                throw new NotImplementedException();
            }

            public bool TryGetProperty(string value, out IUntypedObject result)
            {
                if (_values.TryGetValue(value, out var x))
                {
                    result = new SimpleObject(x);
                    return true;
                }

                result = null;
                return false;
            }
        }
    }
}
