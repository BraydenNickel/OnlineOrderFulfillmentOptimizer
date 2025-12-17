# OnlineOrderFulfillmentOptimizer

# Created by 
Brayden Nickel
Seth Garciano
Kyle Castillo - 904556

# Project Description
The Online Order Fulfillment Optimizer is a program designed to simulate the process of order fulfillment from order to warehouse. It would validate incoming orders, checks warehouse inventory, fulfills the order if able, then updates the warehouse's remaining inventory.
Our solution for handling multiple orders was to implement parallelization to validate orders in multiple threads to fully utilize CPU usage.

# Programming Language and Justification
The program was coded using the C# language. It was decided to use C# for its scalability, ability with outside databases, its wide use in logistics, and its compatibility with the scope of the project. Because of the way it handles compiling and runtime, it can easily catch common errors like missing values or an incorrect data type. It can easily integrate existing databases of logistics companies for easy transfer of data. It bas built in integration and a detailed documentation regarding parallelization and its Error-Handling systems are easy to use and navigate. C# is also widely used by logistics companies for inventory management and order fulfillment.

# System Design and Overview

# Description of parallel component

# Exception Handling Strategy

# Instructions to run the program