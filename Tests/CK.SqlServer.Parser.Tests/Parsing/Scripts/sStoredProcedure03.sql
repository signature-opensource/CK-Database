-- Version = 1.0.0

create proc InvBack.sOfferCreate
(
	@ActorId int,
	
	@Title nvarchar(256),
	@ProjectName nvarchar(256),
	@ClientId int,
	@ContactId int,
	@CompanyLocationId int,

	@OfferIdResult int output
)
as
begin
--[beginsp]
	insert into InvBack.tOffer
	(
		Title,
		ProjectName,

		ClientId,
		ContactId,
		CompanyLocationId,

		Status,

		TotalExclVat,
		VatTypeId,

		ContactFirstName,
		ContactLastName,
	
		ClientName,
		ClientServiceName,
		ClientDepartment,
		ClientAddress,
		ClientZipCode,
		ClientCity,
		ClientCountry,

		OfferDate,
		AcceptDate,
		CancelDate
	) values (
		@Title, -- Title,
		@ProjectName, -- ProjectName

		@ClientId, -- ClientId
		@ContactId, -- ContactId
		@CompanyLocationId, -- CompanyLocationId

		0, -- Status

		0, -- TotalExclVat
		0, -- VatTypeId

		'', -- ContactFirstName
		'', -- ContactLastName

		'', -- ClientName
		'', -- ClientServiceName
		'', -- ClientDepartment
		'', -- ClientAddress
		'', -- ClientZipCode
		'', -- ClientCity
		'', -- ClientCountry

		'', -- OfferDate
		'', -- AcceptDate
		'' -- CancelDate
	);

	set @OfferIdResult = SCOPE_IDENTITY();
--[endsp]
end