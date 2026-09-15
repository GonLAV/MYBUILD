using System.Net;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.Tests.TestExtension.Attributes;
using NUnit.Framework;
using TestBase = Bolt.Automation.InfraTests.TestExtension.Base.TestBase;

namespace Bolt.Automation.InfraTests.InfraTests
{
    [Property("Category", "Demo")]
    public class LoggerCapabilitiesDemo() : TestBase()
    {
        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Property("Description", "Demonstrates basic logging levels")]
        public void BasicLoggingLevelsDemo()
        {
            // Basic logging at different levels
            _logger.Trace("This is a TRACE level message with low-level details");
            _logger.Debug("This is a DEBUG level message with developer information");
            _logger.Info("This is an INFO level message for general test progress");
            _logger.Warning("This is a WARNING level message about potential issues");
            _logger.Error("This is an ERROR level message when something fails");
            _logger.Fatal("This is a FATAL level message for critical failures");

            // With formatted parameters
            _logger.Info("Formatted message with {0} and {1}", "parameters", 123);
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Property("Description", "Demonstrates step tracking with nested steps")]
        public void StepTrackingDemo()
        {
            using (_logger.StartStep("Parent Step"))
            {
                _logger.Info("Inside the parent step");

                using (_logger.StartStep("Child Step 1"))
                {
                    _logger.Info("Inside the first child step");

                    using (_logger.StartStep("Grandchild Step"))
                    {
                        _logger.Info("This is 3 levels deep");
                    }
                }

                using (_logger.StartStep("Child Step 2"))
                {
                    _logger.Info("Inside the second child step");
                }

                _logger.Info("Back to parent step");
            }

            // Direct step management (alternative to using)
            _logger.StartStep("Manually Managed Step");
            _logger.Info("Inside a manually managed step");
            _logger.EndStep();
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Property("Description", "Demonstrates business rule validation logging")]
        public void BusinessRuleValidationDemo()
        {
            using (_logger.StartStep("User Registration Process"))
            {
                // Simulate checking business rules
                _logger.LogBusinessRule("MinimumAge", true, "User must be at least 18 years old");
                _logger.LogBusinessRule("PasswordComplexity", true, "Password must contain at least 8 characters with mixed case and numbers");
                _logger.LogBusinessRule("UniqueEmail", false, "Email address must not be already registered");
                _logger.LogBusinessRule("AcceptedTerms", true, "User must accept terms and conditions");

                // Check multiple rules with step nesting
                using (_logger.StartStep("Address Validation Rules"))
                {
                    _logger.LogBusinessRule("ValidPostalCode", true, "Postal code must be valid for country");
                    _logger.LogBusinessRule("AddressFormat", true, "Address must follow country-specific format");
                }
            }
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Property("Description", "Demonstrates data validation logging")]
        public void DataValidationDemo()
        {
            using (_logger.StartStep("Product Data Validation"))
            {
                // Validate expected vs actual values
                _logger.LogDataValidation(
                    "ProductPrice",
                    true,
                    "120.00",
                    "120.00",
                    "Checking if product price matches expected value"
                );

                _logger.LogDataValidation(
                    "InventoryCount",
                    false,
                    "10",
                    "8",
                    "Verifying inventory count matches expected value"
                );

                // More complex validation with nested steps
                using (_logger.StartStep("Order Validation"))
                {
                    _logger.LogDataValidation(
                        "TotalAmount",
                        true,
                        "246.50",
                        "246.50",
                        "Verifying order total amount"
                    );

                    _logger.LogDataValidation(
                        "ItemCount",
                        true,
                        "5",
                        "5",
                        "Checking number of items in order"
                    );
                }
            }
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Property("Description", "Demonstrates API call logging")]
        public async Task ApiCallLoggingDemo()
        {
            using (_logger.StartStep("API Integration Test"))
            {
                // Create a mock HTTP client
                using (var httpClient = new HttpClient())
                {
                    using (_logger.StartStep("Get User Details"))
                    {
                        // Create request
                        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/users/123");
                        request.Headers.Add("Authorization", "Bearer mock-token");

                        // Mock response
                        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent("{ \"id\": 123, \"name\": \"Test User\", \"email\": \"test@example.com\" }")
                        };

                        // Log API call
                        await _logger.LogApiCallAsync(request, responseMessage, 350);

                        // Business rule validation based on API response
                        _logger.LogBusinessRule("UserExists", true, "User should exist in the system");
                    }

                    using (_logger.StartStep("Create Order"))
                    {
                        // Create request
                        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/orders");
                        request.Headers.Add("Authorization", "Bearer mock-token");
                        request.Content = new StringContent("{ \"userId\": 123, \"products\": [{ \"id\": 456, \"quantity\": 2 }] }");

                        // Mock failure response
                        var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
                        {
                            Content = new StringContent("{ \"error\": \"Insufficient inventory\" }")
                        };

                        // Log API call
                        await _logger.LogApiCallAsync(request, responseMessage, 420);

                        // Business rule validation based on API response
                        _logger.LogBusinessRule("OrderCreation", false, "Order should be created successfully");
                    }
                }
            }
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Property("Description", "Demonstrates UI action logging")]
        public void UiActionLoggingDemo()
        {
            using (_logger.StartStep("User Login Process"))
            {
                // Navigate to login page
                _logger.LogUiAction("Navigate", "LoginPage", "https://example.com/login");

                using (_logger.StartStep("Enter Credentials"))
                {
                    // Enter username
                    _logger.LogUiAction("Input", "UsernameField", "Value: testuser@example.com");

                    // Enter password (sensitive data)
                    _logger.LogUiAction("Input", "PasswordField", "Value: ********");
                }

                // Click login button
                _logger.LogUiAction("Click", "LoginButton", "Submitting login form");

                // Verify dashboard appears
                _logger.LogUiAction("Verify", "Dashboard", "Dashboard is visible after login");

                using (_logger.StartStep("Navigation Menu Interaction"))
                {
                    // Click on profile menu
                    _logger.LogUiAction("Click", "ProfileMenu", "Opening user profile menu");

                    // Click on settings option
                    _logger.LogUiAction("Click", "SettingsOption", "Navigating to settings page");
                }
            }
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Property("Description", "Demonstrates exception logging")]
        public void ExceptionLoggingDemo()
        {
            using (_logger.StartStep("Exception Handling Demo"))
            {
                try
                {
                    _logger.Info("About to simulate a division by zero exception");

                    using (_logger.StartStep("Critical Operation"))
                    {
                        int zero = 0;
                        int result = 10 / zero; // This will throw a DivideByZeroException
                    }
                }
                catch (DivideByZeroException ex)
                {
                    // Log the exception
                    _logger.LogException(ex, "Division operation failed");

                    // Continue with test
                    _logger.Info("Exception was caught and logged");
                }

                try
                {
                    using (_logger.StartStep("Parse Operation"))
                    {
                        _logger.Info("About to simulate a format exception");
                        int number = int.Parse("not-a-number"); // This will throw a FormatException
                    }
                }
                catch (Exception ex)
                {
                    // Log exception with custom message
                    _logger.LogException(ex, "Failed to parse value: {0}", "not-a-number");
                }
            }
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Property("Description", "Demonstrates JSON logging capabilities")]  
        public void JsonLoggingDemo()
        {
            using (_logger.StartStep("JSON Logging Demonstration"))
            {
                // Simple object logging
                var simpleData = new { Name = "John Doe", Age = 30, Active = true };
                _logger.LogJson("Simple object data", simpleData);

                // Complex nested object
                var complexData = new
                {
                    User = new { Id = 123, Email = "user@example.com" },
                    Products = new[]
                    {
                        new { Id = 1, Name = "Widget A", Price = 19.99m },
                        new { Id = 2, Name = "Widget B", Price = 29.99m }
                    },
                    Metadata = new Dictionary<string, object>
                    {
                        ["timestamp"] = DateTime.UtcNow,
                        ["source"] = "web",
                        ["features"] = new[] { "feature1", "feature2" }
                    }
                };
                _logger.LogJsonWithPreview("Complex order data", complexData);

                // Array data
                var arrayData = new[] { "item1", "item2", "item3" };
                _logger.LogJsonStyled("Array of items", arrayData, LogLevel.Info);

                // Error-level JSON logging
                var errorData = new
                {
                    ErrorCode = "VALIDATION_FAILED",
                    Message = "Required fields missing",
                    Fields = new[] { "email", "password" },
                    Timestamp = DateTime.UtcNow
                };
                _logger.LogJson("Validation error details", errorData, LogLevel.Error);

                // Null handling
                _logger.LogJson("Null data test", null);

                // Large object simulation
                var largeObject = new
                {
                    Data = string.Join("", Enumerable.Repeat("LargeDataContent", 100)),
                    Items = Enumerable.Range(1, 50).Select(i => new { Id = i, Value = $"Item{i}" }).ToArray()
                };
                _logger.LogJsonStyled("Large object test", largeObject);
            }
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Property("Description", "Demonstrates complex nested steps with methods")]
        public async Task ComplexStepStructureDemo()
        {
            using (_logger.StartStep("E-commerce Purchase Flow"))
            {
                // Navigate to product
                await NavigateToProductPageAsync();

                // Add to cart
                await AddToCartAsync("Premium Widget");

                // Checkout process
                await ProcessCheckoutAsync();
            }
        }

        // Helper methods with steps to demonstrate nesting across method calls
        private async Task NavigateToProductPageAsync()
        {
            using (_logger.StartStep("Navigate to Product Page"))
            {
                _logger.LogUiAction("Navigate", "ProductListingPage", "https://example.com/products");
                _logger.Info("Waiting for product listing to load");
                await Task.Delay(100); // Simulate waiting

                _logger.LogUiAction("Click", "ProductCard", "Clicking on Premium Widget product");
                await Task.Delay(100); // Simulate waiting

                _logger.LogUiAction("Verify", "ProductDetailPage", "Product detail page loaded");
            }
        }

        private async Task AddToCartAsync(string productName)
        {
            using (_logger.StartStep($"Add {productName} to Cart"))
            {
                _logger.LogUiAction("Verify", "ProductAvailability", "Checking if product is in stock");

                using (_logger.StartStep("Configure Product Options"))
                {
                    _logger.LogUiAction("Select", "ColorOption", "Selected: Blue");
                    _logger.LogUiAction("Select", "SizeOption", "Selected: Large");
                    _logger.LogUiAction("Input", "QuantityField", "Value: 2");
                }

                _logger.LogUiAction("Click", "AddToCartButton", "Adding configured product to cart");
                await Task.Delay(100); // Simulate waiting

                _logger.LogUiAction("Verify", "CartNotification", "Product added to cart notification visible");
            }
        }

        private async Task ProcessCheckoutAsync()
        {
            using (_logger.StartStep("Checkout Process"))
            {
                // Navigate to cart
                using (_logger.StartStep("Navigate to Cart"))
                {
                    _logger.LogUiAction("Click", "CartIcon", "Opening shopping cart");
                    await Task.Delay(100); // Simulate waiting
                    _logger.LogUiAction("Verify", "CartPage", "Cart page loaded");
                }

                // Review items
                using (_logger.StartStep("Review Cart Items"))
                {
                    _logger.LogDataValidation("ItemCount", true, "1", "1", "Verify correct number of items");
                    _logger.LogDataValidation("ProductPrice", true, "$59.99", "$59.99", "Verify correct price");
                    _logger.LogDataValidation("TotalAmount", true, "$119.98", "$119.98", "Verify correct total");
                }

                // Proceed to checkout - this will have its own steps
                await SubmitOrderAsync();
            }
        }

        private async Task SubmitOrderAsync()
        {
            using (_logger.StartStep("Submit Order"))
            {
                using (_logger.StartStep("Enter Shipping Information"))
                {
                    _logger.LogUiAction("Input", "NameField", "Value: John Doe");
                    _logger.LogUiAction("Input", "AddressField", "Value: 123 Test St");
                    _logger.LogUiAction("Input", "CityField", "Value: Testville");
                    _logger.LogUiAction("Select", "CountryDropdown", "Selected: United States");
                    _logger.LogUiAction("Input", "PostalCodeField", "Value: 12345");
                }

                using (_logger.StartStep("Enter Payment Information"))
                {
                    _logger.LogUiAction("Input", "CardNumberField", "Value: **** **** **** 1234");
                    _logger.LogUiAction("Input", "CardHolderField", "Value: John Doe");
                    _logger.LogUiAction("Input", "ExpiryDateField", "Value: 12/25");
                    _logger.LogUiAction("Input", "CVVField", "Value: ***");
                }

                using (_logger.StartStep("Confirm Order"))
                {
                    _logger.LogUiAction("Click", "PlaceOrderButton", "Submitting order");
                    await Task.Delay(200); // Simulate API call

                    // Simulate API call for order submission
                    var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/orders");
                    var response = new HttpResponseMessage(HttpStatusCode.Created)
                    {
                        Content = new StringContent("{ \"orderId\": \"ORD-12345\", \"status\": \"confirmed\" }")
                    };

                    await _logger.LogApiCallAsync(request, response, 543);

                    // Log the response data as JSON
                    var responseData = new { OrderId = "ORD-12345", Status = "confirmed", Total = 119.98m };
                    _logger.LogJsonWithPreview("Order confirmation response", responseData);

                    _logger.LogBusinessRule("OrderSubmission", true, "Order should be submitted successfully");
                    _logger.LogUiAction("Verify", "OrderConfirmationPage", "Order confirmation page displayed");
                }
            }
        }
    }
}