/* 
 * Universalis
 *
 * Welcome to the Universalis documentation page.  <br />  <br />  There is a rate limit of 20 req/s (40 req/s burst) on the API, and 10 req/s (20 req/s burst) on the website itself, if you're scraping instead.  The number of simultaneous connections per IP is capped to 8.    To map item IDs to item names or vice versa, use <a href=\"https://xivapi.com/docs/Search#search\">XIVAPI</a>.  In addition to XIVAPI, you can also get item ID mappings from <a href=\"https://lumina.xiv.dev/docs/intro.html\">Lumina</a>,  <a href=\"https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/Item.csv\">this sheet</a>, or  <a href=\"https://raw.githubusercontent.com/ffxiv-teamcraft/ffxiv-teamcraft/master/apps/client/src/assets/data/items.json\">this</a> pre-made dump.    To get a mapping of world IDs to world names, use <a href=\"https://xivapi.com/World\">XIVAPI</a> or  <a href=\"https://github.com/xivapi/ffxiv-datamining/blob/master/csv/World.csv\">this sheet</a>.  The <code>key</code> column represents the world ID, and the <code>Name</code> column represents the world name.  Note that not all listed worlds are available to be used &#8212; many of the worlds in this sheet are test worlds,  or Korean worlds (Korea is unsupported at this time).    <br />  <br />  If you use this API heavily for your projects, please consider supporting the website on  <a href=\"https://liberapay.com/karashiiro\">Liberapay</a>, <a href=\"https://ko-fi.com/karashiiro\">Ko-fi</a>, or  <a href=\"https://patreon.com/universalis\">Patreon</a>, or making a one-time donation on  <a href=\"https://ko-fi.com/karashiiro\">Ko-fi</a>. Any support is appreciated!  
 *
 * OpenAPI spec version: v2
 * 
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */


using NUnit.Framework;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using IO.Swagger.Api;
using IO.Swagger.Model;
using IO.Swagger.Client;
using System.Reflection;
using Newtonsoft.Json;

namespace IO.Swagger.Test
{
    /// <summary>
    ///  Class for testing UserAlertEventView
    /// </summary>
    /// <remarks>
    /// This file is automatically generated by Swagger Codegen.
    /// Please update the test case below to test the model.
    /// </remarks>
    [TestFixture]
    public class UserAlertEventViewTests
    {
        // TODO uncomment below to declare an instance variable for UserAlertEventView
        //private UserAlertEventView instance;

        /// <summary>
        /// Setup before each test
        /// </summary>
        [SetUp]
        public void Init()
        {
            // TODO uncomment below to create an instance of UserAlertEventView
            //instance = new UserAlertEventView();
        }

        /// <summary>
        /// Clean up after each test
        /// </summary>
        [TearDown]
        public void Cleanup()
        {

        }

        /// <summary>
        /// Test an instance of UserAlertEventView
        /// </summary>
        [Test]
        public void UserAlertEventViewInstanceTest()
        {
            // TODO uncomment below to test "IsInstanceOfType" UserAlertEventView
            //Assert.IsInstanceOfType<UserAlertEventView> (instance, "variable 'instance' is a UserAlertEventView");
        }


        /// <summary>
        /// Test the property 'Id'
        /// </summary>
        [Test]
        public void IdTest()
        {
            // TODO unit test for the property 'Id'
        }
        /// <summary>
        /// Test the property 'AlertID'
        /// </summary>
        [Test]
        public void AlertIDTest()
        {
            // TODO unit test for the property 'AlertID'
        }
        /// <summary>
        /// Test the property 'Timestamp'
        /// </summary>
        [Test]
        public void TimestampTest()
        {
            // TODO unit test for the property 'Timestamp'
        }
        /// <summary>
        /// Test the property 'Data'
        /// </summary>
        [Test]
        public void DataTest()
        {
            // TODO unit test for the property 'Data'
        }

    }

}
