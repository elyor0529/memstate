﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;


namespace Memstate.Tests.DispatchProxy
{

    public class ProxyTest 
	{

	    readonly ITestModel _proxy;
	    readonly Client<ITestModel> _client;

	    public ProxyTest()
	    {
	        var config = new Config();
            ITestModel model = new TestModel();
            var engine = new InMemoryEngineBuilder(config).Build(model);
             _client = new LocalClient<ITestModel>(engine);
            _proxy = _client.GetProxy();
        }

	    [Fact]
	    public void CanSetProperty()
	    {
	        int flag = 0;
	       // _client.CommandExecuted += (sender, args) => flag ++;
	        _proxy.CommandsExecuted = 42;
            Assert.Equal(1, flag);
	    }

		[Fact]
		public void CanExecuteCommandMethod()
		{
			_proxy.IncreaseNumber();
			Assert.Equal(1, _proxy.CommandsExecuted);
		}

		[Fact]
		public void CanExecuteCommandWithResultMethod()
		{
			Assert.Equal(_proxy.Uppercase("livedb"), "LIVEDB");
			Assert.Equal(1, _proxy.CommandsExecuted);
		}

		[Fact]
		public void ThrowsExceptionOnYieldQuery()
		{
		    Assert.ThrowsAny<Exception>(() => _proxy.GetNames().Count());
		}

		[Fact]
		public void CanExecuteQueryMethod()
		{
			var number = _proxy.GetCommandsExecuted();
			Assert.Equal(0, number);
		}

        [Fact]
        public void QueryResultsAreCloned()
        {
            _proxy.AddCustomer("Robert");
            Customer robert = _proxy.GetCustomers().First();
            Customer robert2 = _proxy.GetCustomers().First();
            Assert.NotEqual(robert, robert2);
        }

	    [Fact]
        public void SafeQueryResultsAreNotCloned()
        {
            _proxy.AddCustomer("Robert");
            Customer robert = _proxy.GetCustomersCloned().First();
            Customer robert2 = _proxy.GetCustomersCloned().First();
            Assert.Equal(robert, robert2);
        }

        [Fact]
        public void ResultIsIsolated_attribute_is_recognized()
        {
            var map = MethodMap.MapFor<MethodMapTests.TestModel>();
            var signature = typeof (MethodMapTests.TestModel).GetMethod("GetCustomersCloned").ToString();
            var opInfo = map.GetOperationInfo(signature);
            Assert.True(opInfo.OperationAttribute.Isolation.HasFlag(IsolationLevel.Output));
        }

        [Fact]
        public void GenericQuery()
        {
            var customer = new Customer();
            var clone = _proxy.GenericQuery(customer);
            Assert.NotSame(clone,customer);
            Assert.IsType<Customer>(clone);
        }

        [Fact]
        public void GenericCommand()
        {
            _proxy.GenericCommand(DateTime.Now);
            Assert.Equal(1, _proxy.CommandsExecuted);
        }

        [Fact]
        public void ComplexGeneric()
        {
            double result = _proxy.ComplexGeneric(new KeyValuePair<string, double>("dog", 42.0));
            Assert.Equal(result, 42.0);
            Assert.Equal(1, _proxy.CommandsExecuted);
        }

        [Fact]
        public void Indexer()
        {
            _proxy.AddCustomer("Homer");
            Assert.Equal(1, _proxy.CommandsExecuted);

            var customer = _proxy[0];
            Assert.Equal("Homer", customer.Name);

            customer.Name = "Bart";
            _proxy[0] = customer;
            Assert.Equal(2, _proxy.CommandsExecuted);
            var customers = _proxy.GetCustomers();
            Assert.True(customers.Single().Name == "Bart");
        }

        [Fact]
        public void DefaultArgs()
        {
            var result = _proxy.DefaultArgs(10, 10);
            Assert.Equal(62, result);

            result = _proxy.DefaultArgs(10, 10, 10);
            Assert.Equal(30,result);
        }

        [Fact]
        public void NamedArgs()
        {
            var result = _proxy.DefaultArgs(b: 4, a: 2);
            Assert.Equal(48, result);
        }

        [Fact]
        public void ExplicitGeneric()
        {
            var dt = _proxy.ExplicitGeneric<DateTime>();
            Assert.IsType<DateTime>(dt);
            Assert.Equal(default(DateTime), dt);
        }

        [Fact]
        public void Proxy_throws_InnerException()
        {
            Assert.Throws<CommandAbortedException>(() =>
            {
                _proxy.ThrowCommandAborted();
            });

        }
    }
}
