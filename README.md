# OnlineOrderFulfillmentOptimizer

# Created by 
Brayden Nickel 748211
Seth Garciano 918663
Kyle Castillo - 904556

# Project Description
The Online Order Fulfillment Optimizer is a program designed to simulate the process of order fulfillment from order to warehouse. It would validate incoming orders, checks warehouse inventory, fulfills the order if able, then updates the warehouse's remaining inventory.
Our solution for handling multiple orders was to implement parallelization to validate orders in multiple threads to fully utilize CPU usage.

# Programming Language and Justification
The program was coded using the C# language. It was decided to use C# for its scalability, ability with outside databases, its wide use in logistics, and its compatibility with the scope of the project. Because of the way it handles compiling and runtime, it can easily catch common errors like missing values or an incorrect data type. It can easily integrate existing databases of logistics companies for easy transfer of data. It bas built in integration and a detailed documentation regarding parallelization and its Error-Handling systems are easy to use and navigate. C# is also widely used by logistics companies for inventory management and order fulfillment.

# System Design and Overview

Exception handling files:
ConcurrentValidationException.cs
|-Wrapping unexpected errors during the parallel validations

InvalidOrderException.cs
|- Thrown for null orders, no items, blank product keys, or invalid quantities

NoFulFilmentPathException.cs
|- Thrown for when an order cant be fulfill the order ie: no stock or no allocation path

OutOfStockException.cs
|- Thrown when it does not met the requested quantity of the items ordered

UnknownProductException.cs
|- Thrown when an order references a product that isn’t recognized

WarehouseDoesNotExistException.cs
|-hrown as a safety check if allocation references a warehouse that doesn’t exist. 

Models contains the following classes:
Order.cs
|-Stores order ID and requested items

Warehouse.cs
|-Stores warehouse ID and the inventory of each warehouse

Product.cs
|-Stores the product name and type

FufillmentEngine.cs
|-Validate order
  |-Validate inventory
    |-Assign Allocation Order Efficiently 

Main output File:
Program.cs
|-Initialize the sample data
  |- Calls FufillmentEngine
     |- Process the return from FufillmentEngine
	|-Display the results

# Description of parallel component

How the Parallel programming works is it validates multiple orders on multiple threads instead of validating an order one by one. When is it validating it is checking for exceptions and It adds the failed orders to a list and adds the validated orders to a valid order list. We tested to see if the parallelization was working by seeing our cpu usage with and without it

avg cpu usage of 2-3%
with parallelization 
cpu usage jumps about 8-10%

without parallelization jumps about 20%

# Exception Handling Strategy

some exceptions we include for our program were a
	ConcurrentValidation to handle multiple errors in the threads
	InvalidOrder with orders with wrong order IDs
	NoFulFillmentPath when we cant complete an order
	OutOfStock when the requested quantity is more then what we have
	UnknownProduct when a product has unknown ID or name
	Warehouse does not exist for a warehouse that doesn't have correct ID

# Future Improvements

Some things we could improve on in the future is further testing to ensure parallelization is working. Ensure all exceptions are being handled correctly 

To take it further we could implement a database to store all of our products, orders and warehouse information so our data isn't hard coded in the program and implement a UI for the program so someone could use it instead of it being a simulation

# Instructions to run the program

run program.cs